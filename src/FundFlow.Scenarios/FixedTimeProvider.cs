namespace FundFlow.Scenarios;

/// <summary>Systemzeit mit festem Zeitpunkt für Testfälle und Testansicht.</summary>
public sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    /// <summary>Mittag in Berlin am angegebenen Tag – weit weg von jeder Datumsgrenze.</summary>
    public static FixedTimeProvider AtBerlinNoon(DateOnly date)
    {
        var local = date.ToDateTime(new TimeOnly(12, 0));
        var offset = Domain.Scheduling.BusinessCalendar.BerlinTimeZone.GetUtcOffset(local);
        return new FixedTimeProvider(new DateTimeOffset(local, offset));
    }

    public override DateTimeOffset GetUtcNow() => utcNow.ToUniversalTime();
}
