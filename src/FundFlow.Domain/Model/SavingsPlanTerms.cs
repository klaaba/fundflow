namespace FundFlow.Domain.Model;

/// <summary>Anteil eines Fonds an der Sparrate in ganzen Prozent (E-04).</summary>
public sealed record AllocationLine(string InstrumentId, int Percentage);

/// <summary>
/// Konditionen eines Sparplans: Sparrate, Ausführungstag und Fondsaufteilung.
/// Beschreibt sowohl eine Sparplan-Version als auch den Zielzustand eines Änderungsauftrags.
/// </summary>
public sealed record SavingsPlanTerms(
    decimal MonthlyAmount,
    int ExecutionDay,
    IReadOnlyList<AllocationLine> Allocations)
{
    /// <summary>
    /// Fachlicher Vergleich für BR-15: gleiche Sparrate, gleicher Ausführungstag und
    /// gleiche Aufteilung – unabhängig von der Reihenfolge der Fonds.
    /// </summary>
    public bool HasSameTermsAs(SavingsPlanTerms other) =>
        MonthlyAmount == other.MonthlyAmount
        && ExecutionDay == other.ExecutionDay
        && Normalize(Allocations).SequenceEqual(Normalize(other.Allocations));

    private static IEnumerable<AllocationLine> Normalize(IEnumerable<AllocationLine> lines) =>
        lines.OrderBy(l => l.InstrumentId, StringComparer.Ordinal);
}
