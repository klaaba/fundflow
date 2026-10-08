using FundFlow.Domain.Model;
using FundFlow.Infrastructure.Persistence;
using FundFlow.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;

namespace FundFlow.Infrastructure.Sessions;

/// <summary>
/// Demo-Betrieb (Fachkonzept, Abschnitt 14): eigene Kopie der Musterdaten je Sitzung,
/// Zurücksetzen auf Ausgangsstand A0 und Löschen inaktiver Sitzungen.
/// </summary>
public sealed class DemoSessionService(FundFlowDbContext db, TimeProvider timeProvider)
{
    public static readonly TimeSpan InactivityLimit = TimeSpan.FromHours(24);

    /// <summary>Legt die Sitzung mit Ausgangsstand A0 an, falls sie noch nicht existiert, und vermerkt die Aktivität.</summary>
    public async Task EnsureSessionAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var session = await db.DemoSessions.FindAsync([db.SessionId], cancellationToken);

        if (session is null)
        {
            db.DemoSessions.Add(new DemoSession { Id = db.SessionId, CreatedAt = now, LastSeenAt = now });
            AddInitialState();
        }
        else
        {
            session.LastSeenAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>„Demo zurücksetzen“: alle Daten der Sitzung löschen und Ausgangsstand A0 neu anlegen.</summary>
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Kaskadierendes Löschen über die Sitzungsspalte entfernt alle zugehörigen Datensätze.
        await db.DemoSessions.Where(s => s.Id == db.SessionId).ExecuteDeleteAsync(cancellationToken);
        db.ChangeTracker.Clear();
        await EnsureSessionAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>Löscht Sitzungen, die länger als <see cref="InactivityLimit"/> inaktiv sind.</summary>
    /// <returns>Anzahl der gelöschten Sitzungen.</returns>
    public async Task<int> DeleteInactiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        var threshold = timeProvider.GetUtcNow() - InactivityLimit;

        // Zeitstempel sind als Zahl gespeichert; der Vergleich erfolgt daher nach dem Laden der wenigen Sitzungsköpfe.
        var sessions = await db.DemoSessions.AsNoTracking().ToListAsync(cancellationToken);
        var expired = sessions.Where(s => s.LastSeenAt < threshold).Select(s => s.Id).ToList();
        if (expired.Count == 0)
        {
            return 0;
        }

        return await db.DemoSessions.Where(s => expired.Contains(s.Id)).ExecuteDeleteAsync(cancellationToken);
    }

    /// <summary>Ausgangsstand A0 (Fachkonzept 10.4).</summary>
    private void AddInitialState()
    {
        var initial = DemoData.InitialTerms;

        var version = new SavingsPlanVersion
        {
            VersionNo = 1,
            MonthlyAmount = initial.MonthlyAmount,
            ExecutionDay = initial.ExecutionDay,
            ValidFrom = DemoData.InitialValidFrom,
            ValidTo = null,
            Allocations = initial.Allocations
                .Select(a => new Allocation { InstrumentId = a.InstrumentId, Percentage = a.Percentage })
                .ToList(),
        };

        db.Customers.Add(new Customer
        {
            Name = "Erika Musterfrau",
            Portfolios =
            [
                new Portfolio
                {
                    DepotNumber = "DEMO-000001",
                    Status = "aktiv",
                    SavingsPlans =
                    [
                        new SavingsPlan { PlanNumber = "SP-000001", Status = "aktiv", Versions = [version] },
                    ],
                },
            ],
        });
    }
}
