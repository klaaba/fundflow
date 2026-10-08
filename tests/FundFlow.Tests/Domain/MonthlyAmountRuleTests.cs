using FundFlow.Domain.Rules;
using static FundFlow.Tests.Domain.ValidationTestData;

namespace FundFlow.Tests.Domain;

/// <summary>Sparrate: BR-01, BR-02, BR-09, BR-17.</summary>
public class MonthlyAmountRuleTests
{
    [Theory]
    [InlineData("250,00")]      // TC-01
    [InlineData("25,00")]       // TC-08, Grenzwert BR-01
    [InlineData("10.000,00")]   // TC-11, Grenzwert BR-09
    [InlineData("10000")]
    [InlineData("250")]
    [InlineData("250,5")]
    [InlineData(" 250,00 € ")]
    public void Gueltige_Sparrate_wird_angenommen(string amount)
    {
        AssertValid(Validate(Input(amount: amount)));
    }

    [Theory]
    [InlineData("20,00")]  // TC-02
    [InlineData("24,99")]  // TC-09, Grenzwert
    [InlineData("0")]
    [InlineData("-20")]
    public void BR01_Sparrate_unter_25_Euro_wird_abgelehnt(string amount)
    {
        var result = Validate(Input(amount: amount));

        AssertViolations(result, RuleIds.MinMonthlyAmount);
        var issue = Assert.Single(result.Issues);
        Assert.Equal(InputFields.MonthlyAmount, issue.Field);
        Assert.Equal(RuleMessages.MinMonthlyAmount, issue.Message);
    }

    [Fact]
    public void BR02_TC10_Sparrate_mit_drei_Nachkommastellen_wird_abgelehnt()
    {
        var result = Validate(Input(amount: "250,555"));

        AssertViolations(result, RuleIds.MaxDecimalPlaces);
        Assert.Equal(RuleMessages.MaxDecimalPlaces, Assert.Single(result.Issues).Message);
    }

    [Fact]
    public void BR09_TC12_Sparrate_ueber_10000_Euro_wird_abgelehnt()
    {
        var result = Validate(Input(amount: "10.000,01"));

        AssertViolations(result, RuleIds.MaxMonthlyAmount);
        Assert.Equal(RuleMessages.MaxMonthlyAmount, Assert.Single(result.Issues).Message);
    }

    [Theory]
    [InlineData("abc")]      // TC-26
    [InlineData("")]
    [InlineData(null)]
    [InlineData("250.50")]   // englische Schreibweise – nicht als 25.050 € lesen
    [InlineData("1.23,00")]  // ungültige Tausendergruppe
    [InlineData("12,")]
    public void BR17_nicht_lesbare_Sparrate_wird_als_Formatfehler_gemeldet(string? amount)
    {
        var result = Validate(Input(amount: amount));

        AssertViolations(result, RuleIds.InputFormat);
        var issue = Assert.Single(result.Issues);
        Assert.Equal(InputFields.MonthlyAmount, issue.Field);
        Assert.Equal(RuleMessages.AmountFormat, issue.Message);
    }

    [Fact]
    public void Tausenderpunkte_werden_korrekt_gelesen()
    {
        var result = Validate(Input(amount: "1.234,56"));

        AssertValid(result);
        Assert.Equal(1234.56m, result.Change!.Terms.MonthlyAmount);
    }
}
