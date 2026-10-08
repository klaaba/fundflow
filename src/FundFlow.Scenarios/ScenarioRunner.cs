using System.Globalization;
using FundFlow.Domain.Model;
using FundFlow.Domain.Orders;
using FundFlow.Infrastructure.Orders;
using FundFlow.Infrastructure.Sessions;
using Microsoft.EntityFrameworkCore;

namespace FundFlow.Scenarios;

/// <summary>Eine einzelne Prüfung eines Testfalls mit erwartetem und tatsächlichem Wert.</summary>
public sealed record ScenarioCheck(string Name, string Expected, string Actual, bool Passed);

public sealed record ScenarioResult(TestScenario Scenario, IReadOnlyList<ScenarioCheck> Checks)
{
    public bool Passed => Checks.Count > 0 && Checks.All(c => c.Passed);
}

/// <summary>
/// Führt Testfälle gegen die echte Fachlogik und Datenhaltung aus – jeder Fall mit eigener
/// Datenbank im Arbeitsspeicher, festem Referenzdatum und eigenem Ausgangsstand.
/// </summary>
public static class ScenarioRunner
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");
    private static readonly DateOnly A1Date = new(2026, 10, 8);

    public static async Task<IReadOnlyList<ScenarioResult>> RunAllAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<ScenarioResult>();
        foreach (var scenario in TestCatalog.All)
        {
            results.Add(await RunAsync(scenario, cancellationToken));
        }

        return results;
    }

    public static async Task<ScenarioResult> RunAsync(TestScenario scenario, CancellationToken cancellationToken = default)
    {
        try
        {
            return new ScenarioResult(scenario, await ExecuteAsync(scenario, cancellationToken));
        }
        catch (Exception ex)
        {
            return new ScenarioResult(scenario, [new ScenarioCheck("Ausführung", "ohne Fehler", ex.Message, false)]);
        }
    }

    private static async Task<List<ScenarioCheck>> ExecuteAsync(TestScenario scenario, CancellationToken cancellationToken)
    {
        using var database = new InMemoryFundFlowDatabase();
        var sessionId = Guid.NewGuid();
        var time = FixedTimeProvider.AtBerlinNoon(scenario.ReferenceDate);

        await using (var db = database.CreateContext(sessionId))
        {
            await new DemoSessionService(db, time).EnsureSessionAsync(cancellationToken);
        }

        if (scenario.Start == StartState.A1)
        {
            await CreateStartStateA1Async(database, sessionId, cancellationToken);
        }

        var requestsBefore = await CountRequestsAsync(database, sessionId, cancellationToken);

        SubmitResult result;
        await using (var db = database.CreateContext(sessionId))
        {
            var service = new ChangeRequestService(db, time);
            var planId = await service.GetDemoPlanIdAsync(cancellationToken);
            result = await service.SubmitAsync(planId, scenario.Input, confirmedEffectiveDate: null, cancellationToken);
        }

        await using var verify = database.CreateContext(sessionId);
        var versions = await verify.SavingsPlanVersions.OrderBy(v => v.VersionNo).ToListAsync(cancellationToken);
        var requests = await verify.ChangeRequests.Include(r => r.ChangeLog).ToListAsync(cancellationToken);

        return BuildChecks(scenario.Expected, result, requestsBefore, requests, versions, time);
    }

    /// <summary>Ausgangsstand A1: offener Auftrag CR-2026-000001 über 250,00 €, wirksam ab 15.10.2026.</summary>
    private static async Task CreateStartStateA1Async(InMemoryFundFlowDatabase database, Guid sessionId, CancellationToken cancellationToken)
    {
        await using var db = database.CreateContext(sessionId);
        var service = new ChangeRequestService(db, FixedTimeProvider.AtBerlinNoon(A1Date));
        var planId = await service.GetDemoPlanIdAsync(cancellationToken);
        var input = new Domain.Rules.ChangeRequestInput(
            "250,00", "15", "2026-10-08",
            [new("INS-01", "60"), new("INS-02", "40")]);

        var result = await service.SubmitAsync(planId, input, null, cancellationToken);
        if (result.Request?.RequestNumber != "CR-2026-000001")
        {
            throw new InvalidOperationException("Ausgangsstand A1 konnte nicht hergestellt werden.");
        }
    }

    private static async Task<int> CountRequestsAsync(InMemoryFundFlowDatabase database, Guid sessionId, CancellationToken cancellationToken)
    {
        await using var db = database.CreateContext(sessionId);
        return await db.ChangeRequests.CountAsync(cancellationToken);
    }

    private static List<ScenarioCheck> BuildChecks(
        ScenarioExpectation expected,
        SubmitResult result,
        int requestsBefore,
        IReadOnlyList<ChangeRequest> requests,
        IReadOnlyList<SavingsPlanVersion> versions,
        TimeProvider time)
    {
        var checks = new List<ScenarioCheck>();

        void Compare(string name, string expectedValue, string actualValue) =>
            checks.Add(new(name, expectedValue, actualValue, expectedValue == actualValue));

        Compare("Ergebnis", OutcomeLabel(expected.Outcome), OutcomeLabel(result.Outcome));

        if (expected.Outcome == SubmitOutcome.Rejected)
        {
            Compare("Verletzte Regeln", Join(expected.ViolatedRules.Order()), Join(result.Issues.Select(i => i.RuleId).Distinct().Order()));
            Compare("Gespeicherte Aufträge", requestsBefore.ToString(German), requests.Count.ToString(German));
        }

        var messages = string.Join(" | ", result.Issues.Select(i => i.Message));
        foreach (var text in expected.MessagesContain)
        {
            checks.Add(new($"Meldung enthält „{text}“", text, messages.Length > 0 ? messages : "(keine Meldung)", messages.Contains(text, StringComparison.Ordinal)));
        }

        var request = result.Request;
        var schedule = result.Prepared?.Schedule;

        if (expected.EffectiveDate is { } effectiveDate)
        {
            Compare("Wirksamkeitstermin", Format(effectiveDate), Format(request?.EffectiveDate));
        }

        if (expected.Shifted is { } shifted)
        {
            Compare("Verschiebungshinweis", YesNo(shifted), YesNo(request?.EffectiveDateShifted));
        }

        if (expected.HintContains is { } hint)
        {
            var actualHint = schedule?.ShiftHint ?? "(kein Hinweis)";
            checks.Add(new("Hinweistext", hint, actualHint, actualHint.Contains(hint, StringComparison.Ordinal)));
        }

        if (expected.ChangeLog is { } changeLog)
        {
            Compare("Änderungsprotokoll",
                string.Join("; ", changeLog.Select(c => $"{c.Field}: {c.Old} → {c.New}")),
                string.Join("; ", request?.ChangeLog.Select(c => $"{c.FieldName}: {c.OldValue} → {c.NewValue}") ?? ["(kein Auftrag)"]));
        }

        if (expected.RequestNumber is { } number)
        {
            Compare("Auftragsnummer", number, request?.RequestNumber ?? "(kein Auftrag)");
        }

        if (expected.Status is { } status)
        {
            Compare("Status", status, request is null ? "(kein Auftrag)" : ChangeRequestStatusModel.Label(request.Status));
        }

        if (expected.CreatedAtIsTestTime is true)
        {
            Compare("Zeitstempel", FormatTime(time.GetUtcNow()), request is null ? "(kein Auftrag)" : FormatTime(request.CreatedAt));
        }

        if (expected.StatusHistoryEntries is { } historyEntries)
        {
            Compare("Einträge im Statusverlauf", historyEntries.ToString(German), request?.StatusHistory.Count.ToString(German) ?? "0");
        }

        if (expected.ReplacesRequestNumber is { } replacesNumber)
        {
            var replaced = requests.SingleOrDefault(r => r.Id == request?.ReplacesRequestId);
            Compare("Ersetzt Auftrag", replacesNumber, replaced?.RequestNumber ?? "(keinen)");

            if (expected.ReplacedRequestStatus is { } replacedStatus)
            {
                Compare("Status des ersetzten Auftrags", replacedStatus,
                    replaced is null ? "(keiner)" : ChangeRequestStatusModel.Label(replaced.Status));
            }
        }

        var newVersion = versions.SingleOrDefault(v => v.Id == request?.ResultingVersionId);

        if (expected.NewVersionNo is { } versionNo)
        {
            Compare("Neue Version", versionNo.ToString(German), newVersion?.VersionNo.ToString(German) ?? "(keine)");
        }

        if (expected.NewVersionValidFrom is { } validFrom)
        {
            Compare("Neue Version gültig ab", Format(validFrom), Format(newVersion?.ValidFrom));
        }

        if (expected.PreviousVersionValidTo is { } validTo)
        {
            Compare("Version 1 gültig bis", Format(validTo), Format(versions.FirstOrDefault(v => v.VersionNo == 1)?.ValidTo));
        }

        if (expected.DiscardedVersionNo is { } discardedNo)
        {
            Compare($"Version {discardedNo} verworfen", YesNo(true), YesNo(versions.SingleOrDefault(v => v.VersionNo == discardedNo)?.IsDiscarded));
        }

        return checks;
    }

    private static string OutcomeLabel(SubmitOutcome outcome) => outcome switch
    {
        SubmitOutcome.Created => "Auftrag angelegt",
        SubmitOutcome.Rejected => "abgelehnt",
        SubmitOutcome.EffectiveDateChanged => "Wirksamkeitstermin geändert",
        _ => outcome.ToString(),
    };

    private static string Join(IEnumerable<string> values)
    {
        var text = string.Join(", ", values);
        return text.Length > 0 ? text : "(keine)";
    }

    private static string Format(DateOnly? date) => date?.ToString("dd.MM.yyyy", German) ?? "(kein Datum)";

    private static string FormatTime(DateTimeOffset time) => time.ToUniversalTime().ToString("dd.MM.yyyy HH:mm:ss 'UTC'", German);

    private static string YesNo(bool? value) => value switch
    {
        true => "ja",
        false => "nein",
        null => "(unbekannt)",
    };
}
