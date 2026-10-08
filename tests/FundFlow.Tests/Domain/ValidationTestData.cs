using FundFlow.Domain.Model;
using FundFlow.Domain.Rules;
using FundFlow.Infrastructure.Seed;

namespace FundFlow.Tests.Domain;

/// <summary>Eingaben und Kontext auf Basis von Ausgangsstand A0 und Referenzdatum 08.10.2026 (Fachkonzept 12.1).</summary>
internal static class ValidationTestData
{
    public static readonly DateOnly ReferenceDate = new(2026, 10, 8);

    /// <summary>Ausgangsstand A1: offener Auftrag mit Sparrate 250,00 €.</summary>
    public static readonly OpenRequestReference OpenRequestA1 = new(
        "CR-2026-000001",
        DemoData.InitialTerms with { MonthlyAmount = 250.00m });

    public static ValidationContext ContextA0(DateOnly? today = null) =>
        new(today ?? ReferenceDate, DemoData.InstrumentsById, DemoData.InitialTerms);

    public static ValidationContext ContextA1() =>
        ContextA0() with { OpenRequest = OpenRequestA1 };

    /// <summary>Eingabe, die A0 entspricht; nur die angegebenen Felder weichen ab.</summary>
    public static ChangeRequestInput Input(
        string? amount = "150,00",
        string? day = "15",
        string? requestedFrom = "2026-10-08",
        params (string? InstrumentId, string? Percentage)[] allocations)
    {
        var lines = allocations.Length == 0
            ? [new AllocationInput("INS-01", "60"), new AllocationInput("INS-02", "40")]
            : allocations.Select(a => new AllocationInput(a.InstrumentId, a.Percentage)).ToList();

        return new ChangeRequestInput(amount, day, requestedFrom, lines);
    }

    public static ValidationResult Validate(ChangeRequestInput input, ValidationContext? context = null) =>
        ChangeRequestValidator.Validate(input, context ?? ContextA0());

    /// <summary>Prüft, dass genau die erwarteten Regeln verletzt sind – nicht mehr und nicht weniger.</summary>
    public static void AssertViolations(ValidationResult result, params string[] expectedRuleIds)
    {
        var actual = result.Issues.Select(i => i.RuleId).Distinct().Order();
        Assert.Equal(expectedRuleIds.Distinct().Order(), actual);
        Assert.False(result.IsValid);
        Assert.Null(result.Change);
    }

    public static void AssertValid(ValidationResult result)
    {
        Assert.Empty(result.Issues);
        Assert.True(result.IsValid);
        Assert.NotNull(result.Change);
    }
}
