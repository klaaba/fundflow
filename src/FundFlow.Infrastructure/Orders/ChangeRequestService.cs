using FundFlow.Domain.Model;
using FundFlow.Domain.Orders;
using FundFlow.Domain.Rules;
using FundFlow.Domain.Scheduling;
using FundFlow.Infrastructure.Persistence;
using FundFlow.Infrastructure.Seed;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FundFlow.Infrastructure.Orders;

public enum SubmitOutcome
{
    /// <summary>Auftrag angelegt (BR-08).</summary>
    Created,

    /// <summary>Regelverstöße – kein Auftrag angelegt.</summary>
    Rejected,

    /// <summary>Der Wirksamkeitstermin weicht von der bestätigten Zusammenfassung ab (Prozessschritt 6).</summary>
    EffectiveDateChanged,
}

public sealed record SubmitResult(
    SubmitOutcome Outcome,
    IReadOnlyList<ValidationIssue> Issues,
    PreparedChange? Prepared,
    ChangeRequest? Request);

/// <summary>Zustand eines Sparplans an einem Tag: gültige Version und gegebenenfalls offener Auftrag.</summary>
public sealed record SavingsPlanState(
    SavingsPlan Plan,
    DateOnly Today,
    SavingsPlanVersion CurrentVersion,
    ChangeRequest? OpenRequest)
{
    public ValidationContext ToValidationContext() => new(
        Today,
        DemoData.InstrumentsById,
        CurrentVersion.ToTerms(),
        OpenRequest is null
            ? null
            : new OpenRequestReference(OpenRequest.RequestNumber, OpenRequest.ResultingVersion!.ToTerms()));
}

/// <summary>
/// Verarbeitung von Änderungsaufträgen: Vorbereitung für die Zusammenfassung und Anlage
/// in einer Transaktion (Fachkonzept 8.4, BR-08 und BR-16).
/// </summary>
public sealed class ChangeRequestService(FundFlowDbContext db, TimeProvider timeProvider)
{
    private const int MaxAttempts = 3;

    /// <summary>Der Sparplan der aktuellen Demo-Sitzung.</summary>
    public Task<int> GetDemoPlanIdAsync(CancellationToken cancellationToken = default) =>
        db.SavingsPlans.Select(p => p.Id).SingleAsync(cancellationToken);

    public async Task<SavingsPlanState> LoadStateAsync(int savingsPlanId, CancellationToken cancellationToken = default)
    {
        var today = BusinessCalendar.Today(timeProvider);

        var plan = await db.SavingsPlans
            .Include(p => p.Versions).ThenInclude(v => v.Allocations)
            .Include(p => p.ChangeRequests)
            .AsSplitQuery()
            .SingleAsync(p => p.Id == savingsPlanId, cancellationToken);

        var current = plan.Versions.Single(v => v.IsValidOn(today));
        var open = plan.ChangeRequests.SingleOrDefault(r => r.IsOpenOn(today));

        return new SavingsPlanState(plan, today, current, open);
    }

    /// <summary>Prozessschritte 4 und 5: Prüfung und Zusammenfassung, ohne etwas zu speichern.</summary>
    public async Task<PrepareResult> PrepareAsync(
        int savingsPlanId,
        ChangeRequestInput input,
        CancellationToken cancellationToken = default)
    {
        var state = await LoadStateAsync(savingsPlanId, cancellationToken);
        return ChangePreparer.Prepare(input, state.ToValidationContext());
    }

    /// <summary>
    /// Prozessschritte 6 und 7: erneute Prüfung und Anlage des Auftrags.
    /// </summary>
    /// <param name="confirmedEffectiveDate">
    /// Wirksamkeitstermin aus der bestätigten Zusammenfassung. Weicht der neu ermittelte Termin ab,
    /// wird nichts gespeichert und die neue Zusammenfassung zurückgegeben.
    /// </param>
    public async Task<SubmitResult> SubmitAsync(
        int savingsPlanId,
        ChangeRequestInput input,
        DateOnly? confirmedEffectiveDate,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await TrySubmitAsync(savingsPlanId, input, confirmedEffectiveDate, cancellationToken);
            }
            catch (DbUpdateException ex) when (attempt < MaxAttempts && IsUniqueViolation(ex))
            {
                // Eine parallele Sitzung hat dieselbe Auftragsnummer vergeben – mit frischem Stand erneut versuchen.
                db.ChangeTracker.Clear();
            }
        }
    }

    private async Task<SubmitResult> TrySubmitAsync(
        int savingsPlanId,
        ChangeRequestInput input,
        DateOnly? confirmedEffectiveDate,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Schritt 1: erneute Prüfung und Neuberechnung – erst danach wird eine Auftragsnummer vergeben.
        var state = await LoadStateAsync(savingsPlanId, cancellationToken);
        var preparation = ChangePreparer.Prepare(input, state.ToValidationContext());

        if (preparation.Prepared is not { } prepared)
        {
            return new SubmitResult(SubmitOutcome.Rejected, preparation.Issues, null, null);
        }

        var effectiveDate = prepared.Schedule.EffectiveDate;
        if (confirmedEffectiveDate is { } confirmed && confirmed != effectiveDate)
        {
            return new SubmitResult(SubmitOutcome.EffectiveDateChanged, [], prepared, null);
        }

        var now = timeProvider.GetUtcNow();
        var requestNumber = await NextRequestNumberAsync(state.Today.Year, cancellationToken);

        // Schritt 2: offenen Auftrag ersetzen (BR-16).
        if (state.OpenRequest is { } open)
        {
            open.ChangeStatus(ChangeRequestStatus.Replaced, now, $"Ersetzt durch Auftrag {requestNumber}");
            open.ResultingVersion!.IsDiscarded = true;
        }

        // Schritt 3: gültige Version endet am Tag vor dem Wirksamkeitstermin.
        state.CurrentVersion.ValidTo = effectiveDate.AddDays(-1);

        // Schritt 4: neue Version ab Wirksamkeitstermin.
        var terms = prepared.Change.Terms;
        var newVersion = new SavingsPlanVersion
        {
            SavingsPlanId = state.Plan.Id,
            VersionNo = state.Plan.Versions.Max(v => v.VersionNo) + 1,
            MonthlyAmount = terms.MonthlyAmount,
            ExecutionDay = terms.ExecutionDay,
            ValidFrom = effectiveDate,
            ValidTo = null,
            Allocations = terms.Allocations
                .Select(a => new Allocation { InstrumentId = a.InstrumentId, Percentage = a.Percentage })
                .ToList(),
        };

        // Schritte 5 und 6: Auftrag mit Status, Statusverlauf und Änderungsprotokoll.
        var request = new ChangeRequest
        {
            RequestNumber = requestNumber,
            SavingsPlanId = state.Plan.Id,
            CreatedAt = now,
            RequestedFrom = prepared.Change.RequestedFrom,
            EffectiveDate = effectiveDate,
            EffectiveDateShifted = prepared.Schedule.IsShifted,
            ReplacesRequestId = state.OpenRequest?.Id,
            ResultingVersion = newVersion,
            ChangeLog = prepared.Changes
                .Select(c => new ChangeLogEntry { FieldName = c.FieldName, OldValue = c.OldValue, NewValue = c.NewValue })
                .ToList(),
        };
        request.Open(now, "Auftrag fachlich geprüft und angelegt");

        db.ChangeRequests.Add(request);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new SubmitResult(SubmitOutcome.Created, [], prepared, request);
    }

    /// <summary>Auftragsnummer CR-JJJJ-NNNNNN, fortlaufend je Jahr über alle Sitzungen (Fachkonzept 8.4 und 14).</summary>
    private async Task<string> NextRequestNumberAsync(int year, CancellationToken cancellationToken)
    {
        var prefix = $"CR-{year}-";
        var last = await db.ChangeRequests
            .IgnoreQueryFilters()
            .Where(r => r.RequestNumber.StartsWith(prefix))
            .OrderByDescending(r => r.RequestNumber)
            .Select(r => r.RequestNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var next = last is null ? 1 : int.Parse(last[prefix.Length..]) + 1;
        return $"{prefix}{next:D6}";
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqliteException { SqliteErrorCode: 19 }; // SQLITE_CONSTRAINT
}
