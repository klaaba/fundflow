using System.Reflection;
using FundFlow.Domain.Rules;
using FundFlow.Scenarios;
using FundFlow.Web.Demo;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FundFlow.Web.Pages;

/// <summary>
/// Testansicht (US-04): führt TC-01 bis TC-30 gegen die echte Fachlogik aus – mit festem Referenzdatum
/// und eigener Datenbank, ohne die Demo-Daten zu berühren.
/// </summary>
public class TestansichtModel(TestResultsCache cache) : PageModel
{
    public TestRun Run { get; private set; } = null!;

    public int PassedCount => Run.Results.Count(r => r.Passed);

    /// <summary>Rückverfolgbarkeit Regel → Testfälle (Fachkonzept 12.3).</summary>
    public IReadOnlyList<(string Rule, IReadOnlyList<ScenarioResult> Cases)> Coverage { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Run = await cache.GetAsync();

        var rules = typeof(RuleIds)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral)
            .Select(f => (string)f.GetRawConstantValue()!)
            .Order();

        Coverage = rules
            .Select(rule => (rule, (IReadOnlyList<ScenarioResult>)Run.Results.Where(r => r.Scenario.Rules.Contains(rule)).ToList()))
            .ToList();
    }

    public static string StartLabel(StartState start) => start switch
    {
        StartState.A0 => "A0 – Musterdaten, kein offener Auftrag",
        StartState.A1 => "A1 – offener Auftrag CR-2026-000001 über 250,00 €",
        _ => start.ToString(),
    };
}
