using FundFlow.Domain.Rules;
using FundFlow.Domain.Scheduling;

namespace FundFlow.Domain.Orders;

/// <summary>Geprüfte Änderung mit allem, was die Zusammenfassung „Prüfen und bestätigen“ anzeigt.</summary>
public sealed record PreparedChange(
    ValidatedChange Change,
    EffectiveDateResult Schedule,
    IReadOnlyList<FieldChange> Changes,
    string? ReplacesRequestNumber);

public sealed record PrepareResult(IReadOnlyList<ValidationIssue> Issues, PreparedChange? Prepared)
{
    public bool IsValid => Prepared is not null;
}

/// <summary>
/// Prozessschritte 4 und 5 (Fachkonzept, Abschnitt 7): Prüfung, Wirksamkeitstermin und Änderungen
/// gegenüber der gültigen Version. Reine Fachlogik ohne Datenhaltung.
/// </summary>
public static class ChangePreparer
{
    public static PrepareResult Prepare(ChangeRequestInput input, ValidationContext context)
    {
        var validation = ChangeRequestValidator.Validate(input, context);
        if (validation.Change is not { } change)
        {
            return new PrepareResult(validation.Issues, null);
        }

        // BR-07
        var schedule = EffectiveDateCalculator.Calculate(
            context.Today,
            change.RequestedFrom,
            change.Terms.ExecutionDay,
            context.CurrentTerms.ExecutionDay);

        var changes = ChangeLogBuilder.Build(context.CurrentTerms, change.Terms, context.Instruments);

        // BR-16
        var replaces = context.OpenRequest?.RequestNumber;

        return new PrepareResult([], new PreparedChange(change, schedule, changes, replaces));
    }
}
