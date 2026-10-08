using System.Globalization;
using FundFlow.Domain.Model;

namespace FundFlow.Domain.Orders;

/// <summary>Eine geänderte Angabe mit alter und neuer Ausprägung, so wie sie angezeigt wird.</summary>
public sealed record FieldChange(string FieldName, string OldValue, string NewValue);

/// <summary>
/// Änderungsprotokoll: ein Eintrag je geänderter Angabe, verglichen mit der gültigen Version
/// (Fachkonzept 8.4, Schritt 6, und E-08).
/// </summary>
public static class ChangeLogBuilder
{
    public const string Missing = "–";

    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    public static IReadOnlyList<FieldChange> Build(
        SavingsPlanTerms current,
        SavingsPlanTerms requested,
        IReadOnlyDictionary<string, Instrument> instruments)
    {
        var changes = new List<FieldChange>();

        if (current.MonthlyAmount != requested.MonthlyAmount)
        {
            changes.Add(new("Sparrate", FormatAmount(current.MonthlyAmount), FormatAmount(requested.MonthlyAmount)));
        }

        if (current.ExecutionDay != requested.ExecutionDay)
        {
            changes.Add(new("Ausführungstag", FormatDay(current.ExecutionDay), FormatDay(requested.ExecutionDay)));
        }

        var before = current.Allocations.ToDictionary(a => a.InstrumentId, a => a.Percentage, StringComparer.Ordinal);
        var after = requested.Allocations.ToDictionary(a => a.InstrumentId, a => a.Percentage, StringComparer.Ordinal);

        // Bestehende Fonds in bisheriger Reihenfolge, danach hinzugefügte in Eingabereihenfolge.
        var instrumentIds = current.Allocations.Select(a => a.InstrumentId)
            .Concat(requested.Allocations.Select(a => a.InstrumentId))
            .Distinct(StringComparer.Ordinal);

        foreach (var id in instrumentIds)
        {
            var hadBefore = before.TryGetValue(id, out var oldPercentage);
            var hasAfter = after.TryGetValue(id, out var newPercentage);
            if (hadBefore && hasAfter && oldPercentage == newPercentage)
            {
                continue;
            }

            changes.Add(new(
                AllocationFieldName(id, instruments),
                hadBefore ? FormatPercentage(oldPercentage) : Missing,
                hasAfter ? FormatPercentage(newPercentage) : Missing));
        }

        return changes;
    }

    public static string FormatAmount(decimal amount) => amount.ToString("N2", German) + " €";

    public static string FormatDay(int day) => $"{day}.";

    public static string FormatPercentage(int percentage) => $"{percentage} %";

    private static string AllocationFieldName(string instrumentId, IReadOnlyDictionary<string, Instrument> instruments) =>
        instruments.TryGetValue(instrumentId, out var instrument)
            ? $"Anteil {instrument.Name} ({instrumentId})"
            : $"Anteil {instrumentId}";
}
