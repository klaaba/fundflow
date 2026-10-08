using FundFlow.Domain.Rules;
using FundFlow.Infrastructure.Orders;

namespace FundFlow.Scenarios;

/// <summary>Ausgangsstand eines Testfalls (Fachkonzept 12.1).</summary>
public enum StartState
{
    /// <summary>Musterdaten, kein offener Auftrag.</summary>
    A0,

    /// <summary>A0 zuzüglich offenem Auftrag CR-2026-000001 über 250,00 €.</summary>
    A1,
}

/// <summary>Ein Testfall aus Fachkonzept 12.2 – fachliche Beschreibung, Eingabe und prüfbare Erwartung.</summary>
public sealed record TestScenario(
    string Id,
    string Title,
    IReadOnlyList<string> Rules,
    StartState Start,
    DateOnly ReferenceDate,
    string InputDescription,
    ChangeRequestInput Input,
    string ExpectedDescription,
    ScenarioExpectation Expected);

/// <summary>
/// Prüfbare Erwartung. Nur gesetzte Angaben werden verglichen; jede wird zu einer Prüfung
/// mit erwartetem und tatsächlichem Wert.
/// </summary>
public sealed record ScenarioExpectation
{
    public required SubmitOutcome Outcome { get; init; }

    /// <summary>Bei Ablehnung: genau diese Regeln sind verletzt.</summary>
    public IReadOnlyList<string> ViolatedRules { get; init; } = [];

    /// <summary>Texte, die in den Meldungen vorkommen müssen.</summary>
    public IReadOnlyList<string> MessagesContain { get; init; } = [];

    public DateOnly? EffectiveDate { get; init; }
    public bool? Shifted { get; init; }
    public string? HintContains { get; init; }
    public IReadOnlyList<(string Field, string Old, string New)>? ChangeLog { get; init; }
    public string? RequestNumber { get; init; }
    public string? Status { get; init; }
    public bool? CreatedAtIsTestTime { get; init; }
    public int? StatusHistoryEntries { get; init; }
    public string? ReplacesRequestNumber { get; init; }
    public string? ReplacedRequestStatus { get; init; }
    public int? NewVersionNo { get; init; }
    public DateOnly? NewVersionValidFrom { get; init; }
    public DateOnly? PreviousVersionValidTo { get; init; }
    public int? DiscardedVersionNo { get; init; }
}
