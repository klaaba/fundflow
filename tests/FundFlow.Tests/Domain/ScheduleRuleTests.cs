using FundFlow.Domain.Rules;
using static FundFlow.Tests.Domain.ValidationTestData;

namespace FundFlow.Tests.Domain;

/// <summary>Ausführungstag und Wunschdatum: BR-03, BR-06, BR-14, BR-17.</summary>
public class ScheduleRuleTests
{
    [Theory]
    [InlineData("1")]
    [InlineData("28")]
    public void BR03_zulaessiger_Ausfuehrungstag_wird_angenommen(string day)
    {
        AssertValid(Validate(Input(day: day)));
    }

    [Theory]
    [InlineData("7")]   // TC-05
    [InlineData("0")]
    [InlineData("29")]
    [InlineData("31")]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData(null)]
    public void BR03_unzulaessiger_Ausfuehrungstag_wird_abgelehnt(string? day)
    {
        var result = Validate(Input(day: day));

        AssertViolations(result, RuleIds.ExecutionDay);
        var issue = Assert.Single(result.Issues);
        Assert.Equal(InputFields.ExecutionDay, issue.Field);
        Assert.Equal(RuleMessages.ExecutionDay, issue.Message);
    }

    [Fact]
    public void BR06_TC06_Wunschdatum_in_der_Vergangenheit_wird_abgelehnt()
    {
        var result = Validate(Input(amount: "200,00", requestedFrom: "2026-10-07"));

        AssertViolations(result, RuleIds.RequestedFromNotPast);
        Assert.Equal(RuleMessages.RequestedFromNotPast, Assert.Single(result.Issues).Message);
    }

    [Fact]
    public void BR06_TC18_Wunschdatum_heute_ist_zulaessig()
    {
        var result = Validate(Input(amount: "200,00", requestedFrom: "2026-10-08"));

        AssertValid(result);
        Assert.Equal(ReferenceDate, result.Change!.RequestedFrom);
    }

    [Fact]
    public void BR14_TC22_Wunschdatum_genau_zwoelf_Monate_voraus_ist_zulaessig()
    {
        AssertValid(Validate(Input(amount: "200,00", requestedFrom: "2027-10-08")));
    }

    [Fact]
    public void BR14_TC23_Wunschdatum_mehr_als_zwoelf_Monate_voraus_wird_abgelehnt()
    {
        var result = Validate(Input(amount: "200,00", requestedFrom: "2027-10-09"));

        AssertViolations(result, RuleIds.RequestedFromHorizon);
        Assert.Equal(RuleMessages.RequestedFromHorizon, Assert.Single(result.Issues).Message);
    }

    [Theory]
    [InlineData("08.10.2026")]
    [InlineData("8.10.2026")]
    public void Wunschdatum_in_deutscher_Schreibweise_wird_gelesen(string requestedFrom)
    {
        var result = Validate(Input(amount: "200,00", requestedFrom: requestedFrom));

        AssertValid(result);
        Assert.Equal(ReferenceDate, result.Change!.RequestedFrom);
    }

    [Theory]
    [InlineData("31.02.2027")]
    [InlineData("morgen")]
    [InlineData("")]
    [InlineData(null)]
    public void BR17_nicht_lesbares_Wunschdatum_wird_als_Formatfehler_gemeldet(string? requestedFrom)
    {
        var result = Validate(Input(amount: "200,00", requestedFrom: requestedFrom));

        AssertViolations(result, RuleIds.InputFormat);
        var issue = Assert.Single(result.Issues);
        Assert.Equal(InputFields.RequestedFrom, issue.Field);
        Assert.Equal(RuleMessages.DateFormat, issue.Message);
    }
}
