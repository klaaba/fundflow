namespace FundFlow.Domain.Scheduling;

/// <summary>
/// Fachliches Datum. „Heute“ bezieht sich immer auf die Zeitzone Europe/Berlin
/// (Fachkonzept, Glossar „Referenzdatum“) und wird über <see cref="TimeProvider"/> ermittelt,
/// damit Tests und Testansicht ein festes Datum vorgeben können.
/// </summary>
public static class BusinessCalendar
{
    public static TimeZoneInfo BerlinTimeZone { get; } = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    public static DateOnly Today(TimeProvider timeProvider)
    {
        var berlinNow = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), BerlinTimeZone);
        return DateOnly.FromDateTime(berlinNow.DateTime);
    }
}
