using System.Reflection;
using FundFlow.Domain.Rules;
using FundFlow.Scenarios;

namespace FundFlow.Tests.Scenarios;

/// <summary>
/// Rückverfolgbarkeit Regel ↔ Testfall (Fachkonzept 12.3, Erfolgskriterium 3).
/// Vergleicht den Testkatalog im Code mit den Tabellen im Fachkonzept, damit beide nicht auseinanderlaufen.
/// </summary>
public class TraceabilityTests
{
    private static readonly IReadOnlyList<string> AllRuleIds = typeof(RuleIds)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.IsLiteral)
        .Select(f => (string)f.GetRawConstantValue()!)
        .Order()
        .ToList();

    [Fact]
    public void Es_gibt_die_Regeln_BR01_bis_BR17()
    {
        Assert.Equal(Enumerable.Range(1, 17).Select(n => $"BR-{n:D2}"), AllRuleIds);
    }

    [Fact]
    public void Jede_Regel_ist_mindestens_einem_Testfall_zugeordnet()
    {
        var covered = TestCatalog.All.SelectMany(s => s.Rules).ToHashSet();
        var uncovered = AllRuleIds.Where(id => !covered.Contains(id)).ToList();

        Assert.True(uncovered.Count == 0, $"Ohne Testfall: {string.Join(", ", uncovered)}");
    }

    [Fact]
    public void Testfaelle_verweisen_nur_auf_bekannte_Regeln()
    {
        var unknown = TestCatalog.All
            .SelectMany(s => s.Rules.Where(r => !AllRuleIds.Contains(r)).Select(r => $"{s.Id}: {r}"))
            .ToList();

        Assert.True(unknown.Count == 0, $"Unbekannte Regeln: {string.Join(", ", unknown)}");
    }

    [Fact]
    public void Regelzuordnung_je_Testfall_entspricht_Fachkonzept_12_2()
    {
        var fromConcept = ConceptDocument.ReadTable("### 12.2", "### 12.3", "| TC-")
            .ToDictionary(cells => cells[0], cells => SplitIds(cells[^1]));

        var fromCatalog = TestCatalog.All.ToDictionary(s => s.Id, s => s.Rules.Order().ToList());

        Assert.Equal(fromConcept.Keys.Order(), fromCatalog.Keys.Order());
        foreach (var (id, rules) in fromCatalog)
        {
            Assert.True(rules.SequenceEqual(fromConcept[id]),
                $"{id}: Katalog {string.Join(", ", rules)} – Fachkonzept {string.Join(", ", fromConcept[id])}");
        }
    }

    [Fact]
    public void Rueckverfolgbarkeitsmatrix_entspricht_Fachkonzept_12_3()
    {
        var fromConcept = ConceptDocument.ReadTable("### 12.3", "## 13.", "| BR-")
            .ToDictionary(cells => cells[0], cells => SplitIds(cells[1]));

        var fromCatalog = AllRuleIds.ToDictionary(
            rule => rule,
            rule => TestCatalog.All.Where(s => s.Rules.Contains(rule)).Select(s => s.Id).Order().ToList());

        Assert.Equal(fromCatalog.Keys, fromConcept.Keys.Order());
        foreach (var (rule, cases) in fromCatalog)
        {
            Assert.True(cases.SequenceEqual(fromConcept[rule]),
                $"{rule}: Katalog {string.Join(", ", cases)} – Fachkonzept {string.Join(", ", fromConcept[rule])}");
        }
    }

    private static List<string> SplitIds(string cell) =>
        cell.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Order().ToList();
}

/// <summary>Liest Tabellen aus docs/01_Fachkonzept.md.</summary>
internal static class ConceptDocument
{
    private static readonly Lazy<string[]> Lines = new(() => File.ReadAllLines(FindConcept()));

    /// <summary>Tabellenzeilen zwischen zwei Überschriften, die mit dem Präfix beginnen – als getrimmte Zellen.</summary>
    public static IEnumerable<string[]> ReadTable(string fromHeading, string toHeading, string rowPrefix) =>
        Lines.Value
            .SkipWhile(l => !l.StartsWith(fromHeading, StringComparison.Ordinal))
            .Skip(1)
            .TakeWhile(l => !l.StartsWith(toHeading, StringComparison.Ordinal))
            .Where(l => l.StartsWith(rowPrefix, StringComparison.Ordinal))
            .Select(l => l.Trim('|').Split('|').Select(c => c.Trim()).ToArray());

    private static string FindConcept()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "docs", "01_Fachkonzept.md");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException("docs/01_Fachkonzept.md wurde oberhalb des Testverzeichnisses nicht gefunden.");
    }
}
