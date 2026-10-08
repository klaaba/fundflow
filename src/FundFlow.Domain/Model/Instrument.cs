namespace FundFlow.Domain.Model;

/// <summary>Fiktiver Fonds oder ETF aus dem Fondsuniversum (Fachkonzept 10.3).</summary>
public sealed record Instrument(
    string InstrumentId,
    string IsinLikeId,
    string Name,
    string AssetClass,
    bool SavingsPlanEligible);
