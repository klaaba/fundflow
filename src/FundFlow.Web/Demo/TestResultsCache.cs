using FundFlow.Scenarios;

namespace FundFlow.Web.Demo;

/// <summary>
/// Ergebnisse der Testansicht. Die Testfälle sind deterministisch (festes Referenzdatum, eigene Datenbank),
/// daher genügt eine Ausführung je Programmstart.
/// </summary>
public sealed class TestResultsCache(TimeProvider timeProvider)
{
    private readonly Lazy<Task<TestRun>> _run = new(async () =>
    {
        var results = await ScenarioRunner.RunAllAsync();
        return new TestRun(results, timeProvider.GetUtcNow());
    });

    public Task<TestRun> GetAsync() => _run.Value;
}

public sealed record TestRun(IReadOnlyList<ScenarioResult> Results, DateTimeOffset ExecutedAt);
