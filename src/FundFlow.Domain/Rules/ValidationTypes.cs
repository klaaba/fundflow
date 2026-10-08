using FundFlow.Domain.Model;

namespace FundFlow.Domain.Rules;

/// <summary>
/// Rohe Eingaben des Änderungsformulars. Alle Werte kommen als Text an, damit die
/// Formatprüfung (BR-17) Teil der Fachlogik ist und nicht im Framework verschwindet.
/// </summary>
public sealed record ChangeRequestInput(
    string? MonthlyAmount,
    string? ExecutionDay,
    string? RequestedFrom,
    IReadOnlyList<AllocationInput> Allocations);

public sealed record AllocationInput(string? InstrumentId, string? Percentage);

/// <summary>Offener Auftrag, gegen den BR-15 b prüft.</summary>
public sealed record OpenRequestReference(string RequestNumber, SavingsPlanTerms Terms);

/// <summary>Alles, was die Prüfung außer den Eingaben benötigt.</summary>
public sealed record ValidationContext(
    DateOnly Today,
    IReadOnlyDictionary<string, Instrument> Instruments,
    SavingsPlanTerms CurrentTerms,
    OpenRequestReference? OpenRequest = null);

/// <summary>Namen der Eingabefelder, denen eine Meldung zugeordnet wird.</summary>
public static class InputFields
{
    public const string Form = "";
    public const string MonthlyAmount = "MonthlyAmount";
    public const string ExecutionDay = "ExecutionDay";
    public const string RequestedFrom = "RequestedFrom";
    public const string Allocations = "Allocations";

    public static string AllocationInstrument(int index) => $"Allocations[{index}].InstrumentId";
    public static string AllocationPercentage(int index) => $"Allocations[{index}].Percentage";
}

/// <summary>Ein Regelverstoß mit Regel-ID, betroffenem Feld und Meldungstext.</summary>
public sealed record ValidationIssue(string RuleId, string Field, string Message);

/// <summary>Geprüfte und in Fachwerte überführte Änderung.</summary>
public sealed record ValidatedChange(SavingsPlanTerms Terms, DateOnly RequestedFrom);

/// <summary>Ergebnis der Prüfung: alle Verstöße oder die geprüfte Änderung.</summary>
public sealed record ValidationResult(IReadOnlyList<ValidationIssue> Issues, ValidatedChange? Change)
{
    public bool IsValid => Issues.Count == 0;
}
