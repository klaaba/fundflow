using FundFlow.Domain.Rules;

namespace FundFlow.Domain.Scheduling;

/// <summary>Ergebnis von BR-07.</summary>
/// <param name="EffectiveDate">Erster Ausführungstermin mit den neuen Konditionen.</param>
/// <param name="MissedExecutionDate">
/// Termin ab Wunschdatum, der wegen des Annahmeschlusses nicht mehr erreicht wurde – sonst <c>null</c>.
/// </param>
/// <param name="ShiftHint">Hinweistext bei Verschiebung – sonst <c>null</c>.</param>
public sealed record EffectiveDateResult(DateOnly EffectiveDate, DateOnly? MissedExecutionDate, string? ShiftHint)
{
    public bool IsShifted => MissedExecutionDate is not null;
}

/// <summary>
/// BR-07: Ermittlung des Wirksamkeitstermins unter Berücksichtigung des Annahmeschlusses
/// (Fachkonzept 8.3, Entscheidungen E-01 und E-02). Erwartet bereits geprüfte Eingaben.
/// </summary>
public static class EffectiveDateCalculator
{
    /// <summary>Vorlauf zwischen Auftragseingang und Ausführungstermin in Kalendertagen (E-01).</summary>
    public const int LeadTimeDays = 3;

    /// <param name="today">Heute (Europe/Berlin), siehe <see cref="BusinessCalendar"/>.</param>
    /// <param name="requestedFrom">Wunschdatum der Kundin.</param>
    /// <param name="newExecutionDay">Ausführungstag laut Auftrag.</param>
    /// <param name="currentExecutionDay">Ausführungstag der gültigen Version – bestimmt den Hinweistext.</param>
    public static EffectiveDateResult Calculate(
        DateOnly today,
        DateOnly requestedFrom,
        int newExecutionDay,
        int currentExecutionDay)
    {
        if (!SavingsPlanLimits.AllowedExecutionDays.Contains(newExecutionDay))
        {
            throw new ArgumentOutOfRangeException(nameof(newExecutionDay), newExecutionDay,
                "Der Ausführungstag muss vorab nach BR-03 geprüft sein.");
        }

        // F = max(W, H + V)
        var earliestPossible = Max(requestedFrom, today.AddDays(LeadTimeDays));

        // E = erstes Datum ≥ F mit Tag d
        var effectiveDate = NextExecutionOnOrAfter(earliestPossible, newExecutionDay);

        // Verschiebung: der erste Termin ab Wunschdatum liegt vor E
        var firstAfterRequest = NextExecutionOnOrAfter(requestedFrom, newExecutionDay);
        if (firstAfterRequest >= effectiveDate)
        {
            return new EffectiveDateResult(effectiveDate, null, null);
        }

        var hint = RuleMessages.EffectiveDateShifted(
            firstAfterRequest,
            effectiveDate,
            executionDayChanged: newExecutionDay != currentExecutionDay);

        return new EffectiveDateResult(effectiveDate, firstAfterRequest, hint);
    }

    /// <summary>Erster Termin mit dem Ausführungstag am oder nach dem Datum. Tag 1, 15 und 28 gibt es in jedem Monat.</summary>
    public static DateOnly NextExecutionOnOrAfter(DateOnly date, int executionDay)
    {
        var candidate = new DateOnly(date.Year, date.Month, executionDay);
        return candidate >= date ? candidate : candidate.AddMonths(1);
    }

    private static DateOnly Max(DateOnly a, DateOnly b) => a >= b ? a : b;
}
