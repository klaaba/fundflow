using FundFlow.Domain.Scheduling;
using FundFlow.Scenarios;

namespace FundFlow.Tests.Domain;

/// <summary>„Heute“ wird in Europe/Berlin bestimmt, nicht in UTC.</summary>
public class BusinessCalendarTests
{
    [Theory]
    // UTC-Zeitpunkt                Datum in Berlin
    [InlineData("2026-10-07T21:59:00Z", "2026-10-07")] // Sommerzeit: 23:59 in Berlin
    [InlineData("2026-10-07T22:00:00Z", "2026-10-08")] // Sommerzeit: 00:00 in Berlin
    [InlineData("2026-12-31T22:59:00Z", "2026-12-31")] // Winterzeit: 23:59 in Berlin
    [InlineData("2026-12-31T23:00:00Z", "2027-01-01")] // Winterzeit: 00:00 in Berlin
    public void Heute_richtet_sich_nach_Berliner_Zeit(string utcNow, string expectedToday)
    {
        var timeProvider = new FixedTimeProvider(DateTimeOffset.Parse(utcNow));

        Assert.Equal(DateOnly.Parse(expectedToday), BusinessCalendar.Today(timeProvider));
    }

    [Theory]
    [InlineData("2026-10-08")] // Sommerzeit
    [InlineData("2026-12-15")] // Winterzeit
    public void FixedTimeProvider_liefert_den_angegebenen_Berliner_Tag(string date)
    {
        var timeProvider = FixedTimeProvider.AtBerlinNoon(DateOnly.Parse(date));

        Assert.Equal(DateOnly.Parse(date), BusinessCalendar.Today(timeProvider));
    }
}
