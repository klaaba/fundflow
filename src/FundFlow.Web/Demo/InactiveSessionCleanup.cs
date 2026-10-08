using FundFlow.Infrastructure.Persistence;
using FundFlow.Infrastructure.Sessions;
using Microsoft.EntityFrameworkCore;

namespace FundFlow.Web.Demo;

/// <summary>Löscht stündlich Demo-Sitzungen, die länger als 24 Stunden inaktiv sind (Fachkonzept, Abschnitt 14).</summary>
public sealed class InactiveSessionCleanup(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<InactiveSessionCleanup> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<FundFlowDbContext>>();
                await using var db = new FundFlowDbContext(options, new FixedDemoSessionAccessor(Guid.Empty));

                var deleted = await new DemoSessionService(db, timeProvider).DeleteInactiveSessionsAsync(stoppingToken);
                if (deleted > 0)
                {
                    logger.LogInformation("{Count} inaktive Demo-Sitzungen gelöscht.", deleted);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Aufräumen inaktiver Demo-Sitzungen fehlgeschlagen.");
            }
        }
    }
}
