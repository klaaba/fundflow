using FundFlow.Domain.Model;
using FundFlow.Domain.Orders;
using FundFlow.Infrastructure.Seed;

namespace FundFlow.Tests.Domain;

/// <summary>Änderungsprotokoll (Fachkonzept 8.4, Schritt 6).</summary>
public class ChangeLogBuilderTests
{
    private static readonly SavingsPlanTerms A0 = DemoData.InitialTerms;

    private static IReadOnlyList<(string, string, string)> Build(SavingsPlanTerms requested) =>
        ChangeLogBuilder.Build(A0, requested, DemoData.InstrumentsById)
            .Select(c => (c.FieldName, c.OldValue, c.NewValue))
            .ToList();

    [Fact]
    public void Unveraenderte_Angaben_erzeugen_keinen_Eintrag()
    {
        Assert.Empty(Build(A0 with { Allocations = [new("INS-02", 40), new("INS-01", 60)] }));
    }

    [Fact]
    public void Sparrate_und_Ausfuehrungstag_im_deutschen_Format()
    {
        var changes = Build(A0 with { MonthlyAmount = 10_000m, ExecutionDay = 1 });

        Assert.Equal(
            [("Sparrate", "150,00 €", "10.000,00 €"), ("Ausführungstag", "15.", "1.")],
            changes);
    }

    [Fact]
    public void Entfernter_Fonds_erhaelt_als_neuen_Wert_einen_Strich()
    {
        var changes = Build(A0 with { Allocations = [new("INS-01", 100)] });

        Assert.Equal(
            [
                ("Anteil Demo Welt Aktien ETF (INS-01)", "60 %", "100 %"),
                ("Anteil Demo Europa Aktien ETF (INS-02)", "40 %", "–"),
            ],
            changes);
    }

    [Fact]
    public void Komplett_getauschte_Fonds_erscheinen_erst_bisherige_dann_neue()
    {
        var changes = Build(A0 with { Allocations = [new("INS-04", 70), new("INS-03", 30)] });

        Assert.Equal(
            ["INS-01", "INS-02", "INS-04", "INS-03"],
            changes.Select(c => c.Item1[(c.Item1.LastIndexOf('(') + 1)..^1]));
    }
}
