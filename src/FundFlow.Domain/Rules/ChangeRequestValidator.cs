using FundFlow.Domain.Model;

namespace FundFlow.Domain.Rules;

/// <summary>
/// Prüft einen Änderungsauftrag gegen die Format- und Validierungsregeln
/// BR-01 bis BR-06 und BR-09 bis BR-15, BR-17.
/// Alle Regeln werden gemeinsam ausgewertet; das Ergebnis enthält sämtliche Verstöße (E-10).
/// Ist ein Feld nicht lesbar (BR-17), entfallen die weiteren Prüfungen für dieses Feld.
/// </summary>
public static class ChangeRequestValidator
{
    public static ValidationResult Validate(ChangeRequestInput input, ValidationContext context)
    {
        var issues = new List<ValidationIssue>();

        var amount = ValidateMonthlyAmount(input.MonthlyAmount, issues);
        var executionDay = ValidateExecutionDay(input.ExecutionDay, issues);
        var requestedFrom = ValidateRequestedFrom(input.RequestedFrom, context.Today, issues);
        var allocations = ValidateAllocations(input.Allocations, context.Instruments, issues);

        SavingsPlanTerms? terms = null;
        if (amount is not null && executionDay is not null && allocations is not null)
        {
            terms = new SavingsPlanTerms(amount.Value, executionDay.Value, allocations);
            ValidateMustDiffer(terms, context, issues);
        }

        var change = issues.Count == 0 && terms is not null && requestedFrom is not null
            ? new ValidatedChange(terms, requestedFrom.Value)
            : null;

        return new ValidationResult(issues, change);
    }

    private static decimal? ValidateMonthlyAmount(string? text, List<ValidationIssue> issues)
    {
        const string field = InputFields.MonthlyAmount;

        // BR-17
        if (!InputParser.TryParseAmount(text, out var amount, out var decimalPlaces))
        {
            issues.Add(new(RuleIds.InputFormat, field, RuleMessages.AmountFormat));
            return null;
        }

        // BR-02
        if (decimalPlaces > SavingsPlanLimits.MaxDecimalPlaces)
        {
            issues.Add(new(RuleIds.MaxDecimalPlaces, field, RuleMessages.MaxDecimalPlaces));
        }

        // BR-01
        if (amount < SavingsPlanLimits.MinMonthlyAmount)
        {
            issues.Add(new(RuleIds.MinMonthlyAmount, field, RuleMessages.MinMonthlyAmount));
        }

        // BR-09
        if (amount > SavingsPlanLimits.MaxMonthlyAmount)
        {
            issues.Add(new(RuleIds.MaxMonthlyAmount, field, RuleMessages.MaxMonthlyAmount));
        }

        return amount;
    }

    private static int? ValidateExecutionDay(string? text, List<ValidationIssue> issues)
    {
        // BR-03 – auch nicht lesbare Werte sind kein gültiger Ausführungstag.
        if (!InputParser.TryParseDay(text, out var day) || !SavingsPlanLimits.AllowedExecutionDays.Contains(day))
        {
            issues.Add(new(RuleIds.ExecutionDay, InputFields.ExecutionDay, RuleMessages.ExecutionDay));
            return null;
        }

        return day;
    }

    private static DateOnly? ValidateRequestedFrom(string? text, DateOnly today, List<ValidationIssue> issues)
    {
        const string field = InputFields.RequestedFrom;

        // BR-17
        if (!InputParser.TryParseDate(text, out var requestedFrom))
        {
            issues.Add(new(RuleIds.InputFormat, field, RuleMessages.DateFormat));
            return null;
        }

        // BR-06
        if (requestedFrom < today)
        {
            issues.Add(new(RuleIds.RequestedFromNotPast, field, RuleMessages.RequestedFromNotPast));
        }

        // BR-14
        if (requestedFrom > today.AddMonths(SavingsPlanLimits.MaxMonthsAhead))
        {
            issues.Add(new(RuleIds.RequestedFromHorizon, field, RuleMessages.RequestedFromHorizon));
        }

        return requestedFrom;
    }

    /// <returns>
    /// Die Aufteilung als Fachwerte, wenn alle Anteile ganze Zahlen sind – sonst <c>null</c>.
    /// </returns>
    private static List<AllocationLine>? ValidateAllocations(
        IReadOnlyList<AllocationInput> lines,
        IReadOnlyDictionary<string, Instrument> instruments,
        List<ValidationIssue> issues)
    {
        // BR-11
        if (lines.Count is < SavingsPlanLimits.MinAllocations or > SavingsPlanLimits.MaxAllocations)
        {
            issues.Add(new(RuleIds.AllocationCount, InputFields.Allocations, RuleMessages.AllocationCount));
        }

        var parsed = new List<AllocationLine>();
        var allWholeNumbers = true;
        var sumKnown = true;
        var sum = 0m;

        for (var i = 0; i < lines.Count; i++)
        {
            var instrumentId = lines[i].InstrumentId?.Trim() ?? string.Empty;

            // BR-13
            if (!instruments.TryGetValue(instrumentId, out var instrument))
            {
                issues.Add(new(RuleIds.EligibleInstrument, InputFields.AllocationInstrument(i), RuleMessages.InstrumentUnknown));
            }
            else if (!instrument.SavingsPlanEligible)
            {
                issues.Add(new(RuleIds.EligibleInstrument, InputFields.AllocationInstrument(i),
                    RuleMessages.InstrumentNotEligible(instrument.Name)));
            }

            var percentageField = InputFields.AllocationPercentage(i);

            // BR-10 – auch nicht lesbare Werte sind keine ganzen Prozent.
            if (!InputParser.TryParsePercentage(lines[i].Percentage, out var percentage))
            {
                issues.Add(new(RuleIds.WholePercentage, percentageField, RuleMessages.WholePercentage));
                allWholeNumbers = false;
                sumKnown = false;
                continue;
            }

            sum += percentage;

            if (percentage != decimal.Truncate(percentage))
            {
                issues.Add(new(RuleIds.WholePercentage, percentageField, RuleMessages.WholePercentage));
                allWholeNumbers = false;
                continue;
            }

            // BR-05
            if (percentage <= 0)
            {
                issues.Add(new(RuleIds.PositivePercentage, percentageField, RuleMessages.PositivePercentage));
            }

            parsed.Add(new AllocationLine(instrumentId, (int)percentage));
        }

        // BR-12
        var hasDuplicates = lines
            .Select(l => l.InstrumentId?.Trim() ?? string.Empty)
            .Where(id => id.Length > 0)
            .GroupBy(id => id, StringComparer.Ordinal)
            .Any(g => g.Count() > 1);
        if (hasDuplicates)
        {
            issues.Add(new(RuleIds.NoDuplicateInstrument, InputFields.Allocations, RuleMessages.NoDuplicateInstrument));
        }

        // BR-04 – nur prüfbar, wenn alle Anteile Zahlen sind.
        if (sumKnown && sum != SavingsPlanLimits.RequiredAllocationSum)
        {
            issues.Add(new(RuleIds.AllocationSum, InputFields.Allocations, RuleMessages.AllocationSum(sum)));
        }

        return allWholeNumbers ? parsed : null;
    }

    private static void ValidateMustDiffer(SavingsPlanTerms terms, ValidationContext context, List<ValidationIssue> issues)
    {
        // BR-15 a – Vergleichsbasis ist die gültige Version (E-08).
        if (terms.HasSameTermsAs(context.CurrentTerms))
        {
            issues.Add(new(RuleIds.MustDiffer, InputFields.Form, RuleMessages.SameAsCurrentVersion));
            return;
        }

        // BR-15 b – Schutz vor doppelter Einreichung eines offenen Auftrags.
        if (context.OpenRequest is { } open && terms.HasSameTermsAs(open.Terms))
        {
            issues.Add(new(RuleIds.MustDiffer, InputFields.Form, RuleMessages.SameAsOpenRequest(open.RequestNumber)));
        }
    }
}
