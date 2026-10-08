namespace FundFlow.Domain.Rules;

/// <summary>Kennungen der fachlichen Regeln aus dem Fachkonzept, Abschnitt 8.2.</summary>
public static class RuleIds
{
    public const string MinMonthlyAmount = "BR-01";
    public const string MaxDecimalPlaces = "BR-02";
    public const string ExecutionDay = "BR-03";
    public const string AllocationSum = "BR-04";
    public const string PositivePercentage = "BR-05";
    public const string RequestedFromNotPast = "BR-06";
    public const string EffectiveDate = "BR-07";
    public const string OrderCreation = "BR-08";
    public const string MaxMonthlyAmount = "BR-09";
    public const string WholePercentage = "BR-10";
    public const string AllocationCount = "BR-11";
    public const string NoDuplicateInstrument = "BR-12";
    public const string EligibleInstrument = "BR-13";
    public const string RequestedFromHorizon = "BR-14";
    public const string MustDiffer = "BR-15";
    public const string ReplaceOpenRequest = "BR-16";
    public const string InputFormat = "BR-17";
}
