using System.Text;
using FundFlow.Scenarios;

namespace FundFlow.Tests.Scenarios;

/// <summary>Führt alle Testfälle TC-01 bis TC-30 aus dem gemeinsamen Katalog aus.</summary>
public class ScenarioCatalogTests
{
    public static TheoryData<string> ScenarioIds => [.. TestCatalog.All.Select(s => s.Id)];

    [Theory]
    [MemberData(nameof(ScenarioIds))]
    public async Task Testfall_ist_erfuellt(string id)
    {
        var result = await ScenarioRunner.RunAsync(TestCatalog.Get(id));

        Assert.True(result.Passed, Describe(result));
    }

    [Fact]
    public void Katalog_enthaelt_TC01_bis_TC30_lueckenlos()
    {
        var expected = Enumerable.Range(1, 30).Select(n => $"TC-{n:D2}");

        Assert.Equal(expected, TestCatalog.All.Select(s => s.Id));
    }

    private static string Describe(ScenarioResult result)
    {
        var text = new StringBuilder($"{result.Scenario.Id} – {result.Scenario.Title}{Environment.NewLine}");
        foreach (var check in result.Checks.Where(c => !c.Passed))
        {
            text.AppendLine($"  ✗ {check.Name}: erwartet „{check.Expected}“, tatsächlich „{check.Actual}“");
        }

        return text.ToString();
    }
}
