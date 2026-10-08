using FundFlow.Infrastructure.Persistence;
using FundFlow.Infrastructure.Seed;
using FundFlow.Infrastructure.Sessions;
using FundFlow.Scenarios;
using Microsoft.EntityFrameworkCore;
using static FundFlow.Tests.Domain.ValidationTestData;

namespace FundFlow.Tests.Infrastructure;

/// <summary>Demo-Betrieb (Fachkonzept, Abschnitt 14, E-11): getrennte Sitzungen in einer gemeinsamen Datenbank.</summary>
public sealed class DemoSessionTests : IDisposable
{
    private static readonly DateOnly Oct08 = new(2026, 10, 8);

    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Jede_Sitzung_erhaelt_eigene_Musterdaten_A0()
    {
        var first = await _database.StartSessionAsync(Oct08);
        var second = await _database.StartSessionAsync(Oct08);

        var stateFirst = await first.LoadStateAsync(Oct08);
        var stateSecond = await second.LoadStateAsync(Oct08);

        Assert.NotEqual(stateFirst.Plan.Id, stateSecond.Plan.Id);
        Assert.True(stateFirst.CurrentVersion.ToTerms().HasSameTermsAs(DemoData.InitialTerms));
        Assert.True(stateSecond.CurrentVersion.ToTerms().HasSameTermsAs(DemoData.InitialTerms));
    }

    [Fact]
    public async Task Auftraege_einer_Sitzung_sind_in_anderer_Sitzung_nicht_sichtbar()
    {
        var first = await _database.StartSessionAsync(Oct08);
        var second = await _database.StartSessionAsync(Oct08);

        await first.SubmitAsync(Oct08, Input(amount: "250,00"));

        await using var db = second.Context();
        Assert.Empty(await db.ChangeRequests.ToListAsync());
        Assert.Single(await db.SavingsPlanVersions.ToListAsync());
        Assert.Single(await db.SavingsPlans.ToListAsync());
    }

    [Fact]
    public async Task Auftragsnummern_sind_ueber_alle_Sitzungen_fortlaufend_und_eindeutig()
    {
        var first = await _database.StartSessionAsync(Oct08);
        var second = await _database.StartSessionAsync(Oct08);

        var a = await first.SubmitAsync(Oct08, Input(amount: "250,00"));
        var b = await second.SubmitAsync(Oct08, Input(amount: "250,00"));

        Assert.Equal("CR-2026-000001", a.Request!.RequestNumber);
        Assert.Equal("CR-2026-000002", b.Request!.RequestNumber);
    }

    [Fact]
    public async Task Sitzung_wird_nur_einmal_angelegt()
    {
        var session = await _database.StartSessionAsync(Oct08);

        await session.EnsureAsync(FixedTimeProvider.AtBerlinNoon(Oct08));

        await using var db = session.Context();
        Assert.Single(await db.Customers.ToListAsync());
        Assert.Single(await db.SavingsPlanVersions.ToListAsync());
    }

    [Fact]
    public async Task Zuruecksetzen_stellt_A0_wieder_her_und_laesst_andere_Sitzungen_unberuehrt()
    {
        var session = await _database.StartSessionAsync(Oct08);
        var other = await _database.StartSessionAsync(Oct08);
        await session.SubmitAsync(Oct08, Input(amount: "250,00"));
        await other.SubmitAsync(Oct08, Input(amount: "300,00"));

        await session.ResetAsync(Oct08);

        await using (var db = session.Context())
        {
            Assert.Empty(await db.ChangeRequests.ToListAsync());
            var version = Assert.Single(await db.SavingsPlanVersions.Include(v => v.Allocations).ToListAsync());
            Assert.True(version.ToTerms().HasSameTermsAs(DemoData.InitialTerms));
            Assert.Null(version.ValidTo);
        }

        await using (var db = other.Context())
        {
            Assert.Single(await db.ChangeRequests.ToListAsync());
            Assert.Equal(2, await db.SavingsPlanVersions.CountAsync());
        }
    }

    [Fact]
    public async Task Sitzungen_ueber_24_Stunden_inaktiv_werden_vollstaendig_geloescht()
    {
        var start = new DateTimeOffset(2026, 10, 8, 10, 0, 0, TimeSpan.Zero);
        var inactive = await _database.StartSessionAsync(Oct08);
        await inactive.EnsureAsync(new FixedTimeProvider(start));
        await inactive.SubmitAsync(Oct08, Input(amount: "250,00"));

        var active = await _database.StartSessionAsync(Oct08);
        await active.EnsureAsync(new FixedTimeProvider(start.AddHours(20)));

        int deleted;
        await using (var db = _database.CreateContext(Guid.Empty))
        {
            deleted = await new DemoSessionService(db, new FixedTimeProvider(start.AddHours(25)))
                .DeleteInactiveSessionsAsync();
        }

        Assert.Equal(1, deleted);
        await using (var db = _database.CreateContext(Guid.Empty))
        {
            Assert.Equal([active.SessionId], await db.DemoSessions.Select(s => s.Id).ToListAsync());
            Assert.Equal(0, await CountForSessionAsync(db, inactive.SessionId));
            Assert.True(await CountForSessionAsync(db, active.SessionId) > 0);
        }
    }

    /// <summary>Datensätze einer Sitzung über alle sitzungsbezogenen Tabellen, ohne Sitzungsfilter.</summary>
    private static async Task<int> CountForSessionAsync(FundFlowDbContext db, Guid sessionId)
    {
        bool Of(object e) => (Guid)db.Entry(e).Property(FundFlowDbContext.SessionColumn).CurrentValue! == sessionId;

        var counts = new[]
        {
            (await db.Customers.IgnoreQueryFilters().ToListAsync()).Count(Of),
            (await db.Portfolios.IgnoreQueryFilters().ToListAsync()).Count(Of),
            (await db.SavingsPlans.IgnoreQueryFilters().ToListAsync()).Count(Of),
            (await db.SavingsPlanVersions.IgnoreQueryFilters().ToListAsync()).Count(Of),
            (await db.Allocations.IgnoreQueryFilters().ToListAsync()).Count(Of),
            (await db.ChangeRequests.IgnoreQueryFilters().ToListAsync()).Count(Of),
            (await db.StatusHistory.IgnoreQueryFilters().ToListAsync()).Count(Of),
            (await db.ChangeLog.IgnoreQueryFilters().ToListAsync()).Count(Of),
        };
        return counts.Sum();
    }

}
