using FundFlow.Infrastructure.Persistence;
using FundFlow.Infrastructure.Sessions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FundFlow.Scenarios;

/// <summary>
/// Eigene SQLite-Datenbank im Arbeitsspeicher mit den echten Migrationen. Wird von Testfällen und
/// Testansicht genutzt, damit diese die Demo-Daten nie berühren (US-04, Kriterium 4).
/// </summary>
public sealed class InMemoryFundFlowDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly DbContextOptions<FundFlowDbContext> _options;

    public InMemoryFundFlowDatabase()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<FundFlowDbContext>().UseSqlite(_connection).Options;

        using var db = CreateContext(Guid.Empty);
        db.Database.Migrate();
    }

    public FundFlowDbContext CreateContext(Guid sessionId) =>
        new(_options, new FixedDemoSessionAccessor(sessionId));

    public void Dispose() => _connection.Dispose();
}
