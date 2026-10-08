using FundFlow.Domain.Orders;
using FundFlow.Domain.Rules;
using FundFlow.Infrastructure.Orders;
using FundFlow.Scenarios;
using Microsoft.EntityFrameworkCore;
using static FundFlow.Tests.Domain.ValidationTestData;

namespace FundFlow.Tests.Infrastructure;

/// <summary>Anlage von Aufträgen in einem Schritt (BR-08), Ersetzung (BR-16), Versionen und Protokoll.</summary>
public sealed class ChangeRequestServiceTests : IDisposable
{
    private static readonly DateOnly Oct08 = new(2026, 10, 8);

    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task TC01_Sparrate_aendern_legt_Auftrag_mit_Protokoll_an()
    {
        var session = await _database.StartSessionAsync(Oct08);

        var result = await session.SubmitAsync(Oct08, Input(amount: "250,00"));

        Assert.Equal(SubmitOutcome.Created, result.Outcome);
        var request = result.Request!;
        Assert.Equal(new DateOnly(2026, 10, 15), request.EffectiveDate);
        Assert.Equal(ChangeRequestStatus.BusinessValidated, request.Status);
        var entry = Assert.Single(request.ChangeLog);
        Assert.Equal(("Sparrate", "150,00 €", "250,00 €"), (entry.FieldName, entry.OldValue, entry.NewValue));
    }

    [Fact]
    public async Task TC03_neue_Aufteilung_wird_je_Fonds_protokolliert()
    {
        var session = await _database.StartSessionAsync(Oct08);

        var result = await session.SubmitAsync(Oct08,
            Input(allocations: [("INS-01", "50"), ("INS-02", "30"), ("INS-03", "20")]));

        Assert.Equal(
            [
                ("Anteil Demo Welt Aktien ETF (INS-01)", "60 %", "50 %"),
                ("Anteil Demo Europa Aktien ETF (INS-02)", "40 %", "30 %"),
                ("Anteil Demo Schwellenländer ETF (INS-03)", "–", "20 %"),
            ],
            result.Request!.ChangeLog.Select(e => (e.FieldName, e.OldValue, e.NewValue)));
    }

    [Fact]
    public async Task TC07_Auftrag_erhaelt_Nummer_Zeitstempel_Status_Statusverlauf_und_Version()
    {
        var session = await _database.StartSessionAsync(Oct08);
        var testTime = FixedTimeProvider.AtBerlinNoon(Oct08).GetUtcNow();

        var result = await session.SubmitAsync(Oct08, Input(
            amount: "200,00", day: "1", allocations: [("INS-01", "50"), ("INS-02", "50")]));

        var request = result.Request!;
        Assert.Equal("CR-2026-000001", request.RequestNumber);
        Assert.Equal(testTime, request.CreatedAt);
        Assert.Equal(ChangeRequestStatus.BusinessValidated, request.Status);
        Assert.Null(request.ReplacesRequestId);

        var history = Assert.Single(request.StatusHistory);
        Assert.Null(history.FromStatus);
        Assert.Equal(ChangeRequestStatus.BusinessValidated, history.ToStatus);
        Assert.Equal(testTime, history.ChangedAt);

        await using var db = session.Context();
        var versions = await db.SavingsPlanVersions.Include(v => v.Allocations).OrderBy(v => v.VersionNo).ToListAsync();
        Assert.Equal(2, versions.Count);
        Assert.Equal(new DateOnly(2026, 10, 31), versions[0].ValidTo);
        Assert.Equal(new DateOnly(2026, 11, 1), versions[1].ValidFrom);
        Assert.Null(versions[1].ValidTo);
        Assert.Equal(200.00m, versions[1].MonthlyAmount);
        Assert.Equal(1, versions[1].ExecutionDay);
        Assert.Equal([50, 50], versions[1].Allocations.OrderBy(a => a.InstrumentId).Select(a => a.Percentage));

        var stored = await db.ChangeRequests.SingleAsync();
        Assert.Equal(versions[1].Id, stored.ResultingVersionId);
    }

    [Theory]
    [InlineData("TC-02", "20,00", RuleIds.MinMonthlyAmount)]
    [InlineData("TC-26", "abc", RuleIds.InputFormat)]
    public async Task Abgelehnte_Eingabe_speichert_nichts(string testCase, string amount, string expectedRule)
    {
        var session = await _database.StartSessionAsync(Oct08);

        var result = await session.SubmitAsync(Oct08, Input(amount: amount));

        Assert.True(result.Outcome == SubmitOutcome.Rejected, testCase);
        Assert.Contains(result.Issues, i => i.RuleId == expectedRule);
        await AssertUnchangedA0Async(session);
    }

    [Fact]
    public async Task TC04_Aufteilung_mit_98_Prozent_wird_abgelehnt_und_nichts_gespeichert()
    {
        var session = await _database.StartSessionAsync(Oct08);

        var result = await session.SubmitAsync(Oct08,
            Input(allocations: [("INS-01", "60"), ("INS-02", "30"), ("INS-03", "8")]));

        Assert.Equal(SubmitOutcome.Rejected, result.Outcome);
        Assert.Equal(RuleIds.AllocationSum, Assert.Single(result.Issues).RuleId);
        Assert.Null(result.Request);
        await AssertUnchangedA0Async(session);
    }

    [Fact]
    public async Task TC20_verschobener_Termin_wird_am_Auftrag_vermerkt()
    {
        var oct13 = new DateOnly(2026, 10, 13);
        var session = await _database.StartSessionAsync(oct13);

        var result = await session.SubmitAsync(oct13, Input(amount: "200,00", requestedFrom: "2026-10-13"));

        Assert.Equal(new DateOnly(2026, 11, 15), result.Request!.EffectiveDate);
        Assert.True(result.Request.EffectiveDateShifted);
        Assert.NotNull(result.Prepared!.Schedule.ShiftHint);

        var state = await session.LoadStateAsync(oct13);
        Assert.Equal(new DateOnly(2026, 11, 14), state.CurrentVersion.ValidTo);
    }

    [Fact]
    public async Task TC25_neuer_Auftrag_ersetzt_offenen_Auftrag()
    {
        var session = await _database.StartSessionAsync(Oct08);
        var first = (await session.SubmitAsync(Oct08, Input(amount: "250,00"))).Request!;   // Ausgangsstand A1

        var result = await session.SubmitAsync(Oct08, Input(amount: "300,00"));

        Assert.Equal(SubmitOutcome.Created, result.Outcome);
        var second = result.Request!;
        Assert.Equal("CR-2026-000002", second.RequestNumber);
        Assert.Equal(first.Id, second.ReplacesRequestId);
        Assert.Equal("CR-2026-000001", result.Prepared!.ReplacesRequestNumber);

        // Protokoll vergleicht mit der gültigen Version, nicht mit dem ersetzten Auftrag (E-08).
        var entry = Assert.Single(second.ChangeLog);
        Assert.Equal(("150,00 €", "300,00 €"), (entry.OldValue, entry.NewValue));

        await using var db = session.Context();
        var replaced = await db.ChangeRequests.SingleAsync(r => r.Id == first.Id);
        Assert.Equal(ChangeRequestStatus.Replaced, replaced.Status);
        Assert.Equal(2, replaced.StatusHistory.Count);
        var last = replaced.StatusHistory.OrderBy(h => h.Id).Last();
        Assert.Equal(ChangeRequestStatus.BusinessValidated, last.FromStatus);
        Assert.Equal(ChangeRequestStatus.Replaced, last.ToStatus);
        Assert.Equal("Ersetzt durch Auftrag CR-2026-000002", last.Reason);

        var versions = await db.SavingsPlanVersions.OrderBy(v => v.VersionNo).ToListAsync();
        Assert.Equal([1, 2, 3], versions.Select(v => v.VersionNo));
        Assert.Equal(new DateOnly(2026, 10, 14), versions[0].ValidTo);
        Assert.True(versions[1].IsDiscarded);
        Assert.False(versions[2].IsDiscarded);
        Assert.Equal(new DateOnly(2026, 10, 15), versions[2].ValidFrom);
        Assert.Equal(300.00m, versions[2].MonthlyAmount);
    }

    [Fact]
    public async Task Vorbereitung_nennt_den_zu_ersetzenden_Auftrag()
    {
        var session = await _database.StartSessionAsync(Oct08);
        await session.SubmitAsync(Oct08, Input(amount: "250,00"));

        var result = await session.PrepareAsync(Oct08, Input(amount: "300,00"));

        Assert.Equal("CR-2026-000001", result.Prepared!.ReplacesRequestNumber);
    }

    [Fact]
    public async Task TC28_Eingabe_gleich_offenem_Auftrag_wird_abgelehnt()
    {
        var session = await _database.StartSessionAsync(Oct08);
        await session.SubmitAsync(Oct08, Input(amount: "250,00"));

        var result = await session.SubmitAsync(Oct08, Input(amount: "250,00"));

        Assert.Equal(SubmitOutcome.Rejected, result.Outcome);
        Assert.Equal(RuleMessages.SameAsOpenRequest("CR-2026-000001"), Assert.Single(result.Issues).Message);
    }

    [Fact]
    public async Task TC29_Eingabe_gleich_gueltiger_Version_wird_bei_offenem_Auftrag_abgelehnt()
    {
        var session = await _database.StartSessionAsync(Oct08);
        await session.SubmitAsync(Oct08, Input(amount: "250,00"));

        var result = await session.SubmitAsync(Oct08, Input(amount: "150,00"));

        Assert.Equal(SubmitOutcome.Rejected, result.Outcome);
        Assert.Equal(RuleMessages.SameAsCurrentVersion, Assert.Single(result.Issues).Message);
    }

    [Fact]
    public async Task Geaenderter_Termin_zwischen_Zusammenfassung_und_Absenden_speichert_nichts()
    {
        // Zusammenfassung am 12.10. zeigt den 15.10.; abgeschickt wird erst am 13.10. (Annahmeschluss verpasst).
        var session = await _database.StartSessionAsync(new DateOnly(2026, 10, 12));
        var input = Input(amount: "200,00", requestedFrom: "2026-10-13");
        var summary = await session.PrepareAsync(new DateOnly(2026, 10, 12), input);
        Assert.Equal(new DateOnly(2026, 10, 15), summary.Prepared!.Schedule.EffectiveDate);

        var result = await session.SubmitAsync(new DateOnly(2026, 10, 13), input, summary.Prepared.Schedule.EffectiveDate);

        Assert.Equal(SubmitOutcome.EffectiveDateChanged, result.Outcome);
        Assert.Equal(new DateOnly(2026, 11, 15), result.Prepared!.Schedule.EffectiveDate);
        Assert.Null(result.Request);
        await AssertUnchangedA0Async(session);
    }

    [Fact]
    public async Task Wunschdatum_heute_ist_nach_Mitternacht_Vergangenheit_und_wird_beim_Absenden_abgelehnt()
    {
        var session = await _database.StartSessionAsync(new DateOnly(2026, 10, 12));
        var input = Input(amount: "200,00", requestedFrom: "2026-10-12");
        Assert.True((await session.PrepareAsync(new DateOnly(2026, 10, 12), input)).IsValid);

        var result = await session.SubmitAsync(new DateOnly(2026, 10, 13), input, new DateOnly(2026, 10, 15));

        Assert.Equal(SubmitOutcome.Rejected, result.Outcome);
        Assert.Equal(RuleIds.RequestedFromNotPast, Assert.Single(result.Issues).RuleId);
        await AssertUnchangedA0Async(session);
    }

    [Fact]
    public async Task Nach_dem_Wirksamkeitstermin_gilt_die_neue_Version_und_der_Auftrag_ist_nicht_mehr_offen()
    {
        var session = await _database.StartSessionAsync(Oct08);
        var first = (await session.SubmitAsync(Oct08, Input(amount: "250,00"))).Request!; // wirksam ab 15.10.
        var oct16 = new DateOnly(2026, 10, 16);

        var state = await session.LoadStateAsync(oct16);
        Assert.Equal(2, state.CurrentVersion.VersionNo);
        Assert.Null(state.OpenRequest);

        var result = await session.SubmitAsync(oct16, Input(amount: "300,00", requestedFrom: "2026-10-16"));

        Assert.Null(result.Request!.ReplacesRequestId);
        Assert.Equal(("250,00 €", "300,00 €"), (result.Request.ChangeLog[0].OldValue, result.Request.ChangeLog[0].NewValue));

        await using var db = session.Context();
        var stillValidated = await db.ChangeRequests.SingleAsync(r => r.Id == first.Id);
        Assert.Equal(ChangeRequestStatus.BusinessValidated, stillValidated.Status);
    }

    private static async Task AssertUnchangedA0Async(DemoSessionHandle session)
    {
        await using var db = session.Context();
        Assert.Empty(await db.ChangeRequests.ToListAsync());
        Assert.Empty(await db.ChangeLog.ToListAsync());
        var version = Assert.Single(await db.SavingsPlanVersions.ToListAsync());
        Assert.Null(version.ValidTo);
        Assert.Equal(150.00m, version.MonthlyAmount);
    }
}
