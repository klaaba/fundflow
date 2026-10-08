using FundFlow.Domain.Scheduling;

namespace FundFlow.Tests.Domain;

/// <summary>BR-07: Wirksamkeitstermin und Verschiebungshinweis (Fachkonzept 8.3).</summary>
public class EffectiveDateCalculatorTests
{
    private const int CurrentDay = 15; // Ausführungstag in A0

    private static EffectiveDateResult Calculate(string today, string requestedFrom, int executionDay) =>
        EffectiveDateCalculator.Calculate(
            DateOnly.Parse(today), DateOnly.Parse(requestedFrom), executionDay, CurrentDay);

    [Theory]
    // Test-ID           heute         Wunschdatum   Tag  erwarteter Termin
    [InlineData("TC-18", "2026-10-08", "2026-10-08", 15, "2026-10-15")]
    [InlineData("TC-19", "2026-10-12", "2026-10-12", 15, "2026-10-15")] // Grenzwert Annahmeschluss
    [InlineData("TC-21", "2026-10-08", "2026-10-20", 1, "2026-11-01")]
    [InlineData("TC-22", "2026-10-08", "2027-10-08", 15, "2027-10-15")]
    [InlineData("TC-07", "2026-10-08", "2026-10-08", 1, "2026-11-01")]
    [InlineData("Wunschdatum am Ausführungstag", "2026-10-08", "2026-10-15", 15, "2026-10-15")]
    [InlineData("Februar hat den 28.", "2027-02-20", "2027-02-20", 28, "2027-02-28")]
    public void Termin_ohne_Verschiebung(string testCase, string today, string requestedFrom, int day, string expected)
    {
        var result = Calculate(today, requestedFrom, day);

        Assert.True(DateOnly.Parse(expected) == result.EffectiveDate, testCase);
        Assert.False(result.IsShifted, testCase);
        Assert.Null(result.ShiftHint);
    }

    [Fact]
    public void TC20_Annahmeschluss_verpasst_verschiebt_auf_den_Folgemonat()
    {
        var result = Calculate("2026-10-13", "2026-10-13", 15);

        Assert.Equal(new DateOnly(2026, 11, 15), result.EffectiveDate);
        Assert.Equal(new DateOnly(2026, 10, 15), result.MissedExecutionDate);
        Assert.Equal(
            "Ihr Auftrag geht nach dem Annahmeschluss für den 15.10.2026 ein. " +
            "Die Änderung wird daher erst zum 15.11.2026 wirksam; " +
            "die Ausführung am 15.10.2026 erfolgt noch zu den bisherigen Konditionen.",
            result.ShiftHint);
    }

    [Fact]
    public void TC30_Verschiebung_bei_geaendertem_Ausfuehrungstag_nennt_keinen_alten_Ausfuehrungstermin()
    {
        // Am 01.11. findet nach altem Plan (Tag 15) keine Ausführung statt.
        var result = Calculate("2026-10-30", "2026-10-30", 1);

        Assert.Equal(new DateOnly(2026, 12, 1), result.EffectiveDate);
        Assert.Equal(new DateOnly(2026, 11, 1), result.MissedExecutionDate);
        Assert.Equal(
            "Ihr Auftrag geht nach dem Annahmeschluss für den 01.11.2026 ein. " +
            "Die Änderung wird daher erst zum 01.12.2026 wirksam; " +
            "bis dahin wird Ihr Sparplan zu den bisherigen Konditionen ausgeführt.",
            result.ShiftHint);
    }

    [Fact]
    public void Verschiebung_ueber_den_Jahreswechsel()
    {
        var result = Calculate("2026-12-30", "2026-12-30", 1);

        Assert.Equal(new DateOnly(2027, 2, 1), result.EffectiveDate);
        Assert.Equal(new DateOnly(2027, 1, 1), result.MissedExecutionDate);
    }

    [Fact]
    public void Spaetes_Wunschdatum_verhindert_eine_Verschiebung()
    {
        // Heute ist der Annahmeschluss für den 15.10. verpasst, die Kundin wünscht aber ohnehin erst ab 01.11.
        var result = Calculate("2026-10-13", "2026-11-01", 15);

        Assert.Equal(new DateOnly(2026, 11, 15), result.EffectiveDate);
        Assert.False(result.IsShifted);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(31)]
    public void Ungepruefter_Ausfuehrungstag_wird_zurueckgewiesen(int day)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Calculate("2026-10-08", "2026-10-08", day));
    }
}
