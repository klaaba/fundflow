namespace FundFlow.Domain.Rules;

/// <summary>Grenzwerte der fachlichen Regeln (Fachkonzept 8.2 und Entscheidungen E-03 bis E-06).</summary>
public static class SavingsPlanLimits
{
    public const decimal MinMonthlyAmount = 25.00m;      // BR-01
    public const int MaxDecimalPlaces = 2;               // BR-02
    public static readonly IReadOnlySet<int> AllowedExecutionDays = new HashSet<int> { 1, 15, 28 }; // BR-03
    public const int RequiredAllocationSum = 100;        // BR-04
    public const decimal MaxMonthlyAmount = 10_000.00m;  // BR-09, E-05
    public const int MinAllocations = 1;                 // BR-11
    public const int MaxAllocations = 5;                 // BR-11, E-03
    public const int MaxMonthsAhead = 12;                // BR-14, E-06
}
