using FundFlow.Domain.Model;

namespace FundFlow.Infrastructure.Seed;

/// <summary>
/// Fiktive Stamm- und Musterdaten gemäß Fachkonzept 10.3 und 10.4.
/// Kennungen mit „XX“ kommen in echten ISINs nicht vor.
/// </summary>
public static class DemoData
{
    public static IReadOnlyList<Instrument> Instruments { get; } =
    [
        new("INS-01", "XXDEMO000011", "Demo Welt Aktien ETF", "Aktien-ETF", true),
        new("INS-02", "XXDEMO000029", "Demo Europa Aktien ETF", "Aktien-ETF", true),
        new("INS-03", "XXDEMO000037", "Demo Schwellenländer ETF", "Aktien-ETF", true),
        new("INS-04", "XXDEMO000045", "Demo Euro Staatsanleihen Fonds", "Rentenfonds", true),
        new("INS-05", "XXDEMO000052", "Demo Unternehmensanleihen ETF", "Renten-ETF", true),
        new("INS-06", "XXDEMO000060", "Demo Mischfonds Ausgewogen", "Mischfonds", true),
        new("INS-07", "XXDEMO000078", "Demo Immobilienfonds", "Offener Immobilienfonds", false),
    ];

    public static IReadOnlyDictionary<string, Instrument> InstrumentsById { get; } =
        Instruments.ToDictionary(i => i.InstrumentId, StringComparer.Ordinal);

    /// <summary>Konditionen der Version 1 im Ausgangsstand A0.</summary>
    public static SavingsPlanTerms InitialTerms { get; } = new(
        MonthlyAmount: 150.00m,
        ExecutionDay: 15,
        Allocations: [new("INS-01", 60), new("INS-02", 40)]);

    public static DateOnly InitialValidFrom { get; } = new(2026, 1, 15);
}
