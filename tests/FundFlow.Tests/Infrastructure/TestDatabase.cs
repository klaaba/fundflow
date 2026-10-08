using FundFlow.Domain.Orders;
using FundFlow.Domain.Rules;
using FundFlow.Infrastructure.Orders;
using FundFlow.Infrastructure.Persistence;
using FundFlow.Infrastructure.Sessions;
using FundFlow.Scenarios;

namespace FundFlow.Tests.Infrastructure;

/// <summary>
/// Testdatenbank auf Basis von <see cref="InMemoryFundFlowDatabase"/>. Jede Aktion läuft – wie eine
/// Webanfrage – mit einem eigenen Kontext.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    private readonly InMemoryFundFlowDatabase _database = new();

    public FundFlowDbContext CreateContext(Guid sessionId) => _database.CreateContext(sessionId);

    /// <summary>Neue Demo-Sitzung mit Ausgangsstand A0.</summary>
    public async Task<DemoSessionHandle> StartSessionAsync(DateOnly today)
    {
        var handle = new DemoSessionHandle(this, Guid.NewGuid());
        await handle.EnsureAsync(FixedTimeProvider.AtBerlinNoon(today));
        return handle;
    }

    public void Dispose() => _database.Dispose();
}

/// <summary>Zugriff auf eine Demo-Sitzung in Tests.</summary>
public sealed class DemoSessionHandle(TestDatabase database, Guid sessionId)
{
    public Guid SessionId { get; } = sessionId;

    public FundFlowDbContext Context() => database.CreateContext(SessionId);

    public async Task EnsureAsync(TimeProvider time)
    {
        await using var db = Context();
        await new DemoSessionService(db, time).EnsureSessionAsync();
    }

    public async Task ResetAsync(DateOnly today)
    {
        await using var db = Context();
        await new DemoSessionService(db, FixedTimeProvider.AtBerlinNoon(today)).ResetAsync();
    }

    public Task<SubmitResult> SubmitAsync(DateOnly today, ChangeRequestInput input, DateOnly? confirmedEffectiveDate = null) =>
        WithServiceAsync(today, async (service, planId) => await service.SubmitAsync(planId, input, confirmedEffectiveDate));

    public Task<PrepareResult> PrepareAsync(DateOnly today, ChangeRequestInput input) =>
        WithServiceAsync(today, async (service, planId) => await service.PrepareAsync(planId, input));

    public Task<SavingsPlanState> LoadStateAsync(DateOnly today) =>
        WithServiceAsync(today, async (service, planId) => await service.LoadStateAsync(planId));

    private async Task<T> WithServiceAsync<T>(DateOnly today, Func<ChangeRequestService, int, Task<T>> action)
    {
        await using var db = Context();
        var service = new ChangeRequestService(db, FixedTimeProvider.AtBerlinNoon(today));
        var planId = await service.GetDemoPlanIdAsync();
        return await action(service, planId);
    }
}
