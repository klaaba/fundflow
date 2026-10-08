using FundFlow.Domain.Model;
using FundFlow.Domain.Rules;
using FundFlow.Web.Demo;

namespace FundFlow.Web.Pages.Sparplan;

/// <summary>Formularwerte der Sparplanänderung. Alle Werte bleiben Text, geprüft wird in der Fachlogik (BR-17).</summary>
public sealed class ChangeForm
{
    public string? MonthlyAmount { get; set; }
    public string? ExecutionDay { get; set; }
    public string? RequestedFrom { get; set; }
    public List<AllocationRow> Allocations { get; set; } = [];

    public static ChangeForm From(SavingsPlanTerms terms, DateOnly requestedFrom) => new()
    {
        MonthlyAmount = Display.AmountInput(terms.MonthlyAmount),
        ExecutionDay = terms.ExecutionDay.ToString(System.Globalization.CultureInfo.InvariantCulture),
        RequestedFrom = requestedFrom.ToString("yyyy-MM-dd"),
        Allocations = terms.Allocations
            .Select(a => new AllocationRow { InstrumentId = a.InstrumentId, Percentage = a.Percentage.ToString(System.Globalization.CultureInfo.InvariantCulture) })
            .ToList(),
    };

    /// <summary>
    /// Völlig leere Zeilen (weder Fonds noch Anteil) entfernt die Oberfläche vor der Prüfung –
    /// sie entstehen nur durch „Fonds hinzufügen“ ohne Eingabe und sind keine fachliche Angabe.
    /// </summary>
    public void RemoveEmptyRows() =>
        Allocations.RemoveAll(r => string.IsNullOrWhiteSpace(r.InstrumentId) && string.IsNullOrWhiteSpace(r.Percentage));

    public ChangeRequestInput ToInput() => new(
        MonthlyAmount,
        ExecutionDay,
        RequestedFrom,
        Allocations.Select(a => new AllocationInput(a.InstrumentId, a.Percentage)).ToList());
}

public sealed class AllocationRow
{
    public string? InstrumentId { get; set; }
    public string? Percentage { get; set; }
}
