using FundFlow.Domain.Model;
using FundFlow.Domain.Rules;
using static FundFlow.Tests.Domain.ValidationTestData;

namespace FundFlow.Tests.Domain;

/// <summary>Fondsaufteilung: BR-04, BR-05, BR-10, BR-11, BR-12, BR-13.</summary>
public class AllocationRuleTests
{
    [Fact]
    public void TC03_neue_Aufteilung_mit_drittem_Fonds_wird_angenommen()
    {
        var result = Validate(Input(allocations: [("INS-01", "50"), ("INS-02", "30"), ("INS-03", "20")]));

        AssertValid(result);
        Assert.Equal(
            [new AllocationLine("INS-01", 50), new AllocationLine("INS-02", 30), new AllocationLine("INS-03", 20)],
            result.Change!.Terms.Allocations);
    }

    [Fact]
    public void BR04_TC04_Aufteilung_mit_98_Prozent_wird_abgelehnt()
    {
        var result = Validate(Input(allocations: [("INS-01", "60"), ("INS-02", "30"), ("INS-03", "8")]));

        AssertViolations(result, RuleIds.AllocationSum);
        var issue = Assert.Single(result.Issues);
        Assert.Equal(InputFields.Allocations, issue.Field);
        Assert.Equal(
            "Die Fondsaufteilung ergibt aktuell 98 %. Bitte passen Sie die Anteile auf insgesamt 100 % an.",
            issue.Message);
    }

    [Fact]
    public void BR04_Aufteilung_ueber_100_Prozent_wird_abgelehnt()
    {
        var result = Validate(Input(allocations: [("INS-01", "60"), ("INS-02", "41")]));

        AssertViolations(result, RuleIds.AllocationSum);
        Assert.Contains("101 %", Assert.Single(result.Issues).Message);
    }

    [Fact]
    public void BR05_TC13_Fonds_mit_0_Prozent_wird_abgelehnt()
    {
        var result = Validate(Input(allocations: [("INS-01", "60"), ("INS-02", "40"), ("INS-03", "0")]));

        AssertViolations(result, RuleIds.PositivePercentage);
        var issue = Assert.Single(result.Issues);
        Assert.Equal(InputFields.AllocationPercentage(2), issue.Field);
        Assert.Equal(RuleMessages.PositivePercentage, issue.Message);
    }

    [Fact]
    public void BR10_TC14_Anteile_mit_Nachkommastellen_werden_abgelehnt()
    {
        var result = Validate(Input(allocations: [("INS-01", "33,5"), ("INS-02", "66,5")]));

        AssertViolations(result, RuleIds.WholePercentage);
        Assert.Equal(
            [InputFields.AllocationPercentage(0), InputFields.AllocationPercentage(1)],
            result.Issues.Select(i => i.Field));
    }

    [Fact]
    public void BR10_nicht_lesbarer_Anteil_wird_abgelehnt_und_Summe_nicht_geprueft()
    {
        var result = Validate(Input(allocations: [("INS-01", "abc"), ("INS-02", "40")]));

        AssertViolations(result, RuleIds.WholePercentage);
    }

    [Fact]
    public void BR10_Prozentzeichen_in_der_Eingabe_ist_erlaubt()
    {
        AssertValid(Validate(Input(allocations: [("INS-01", "50 %"), ("INS-02", "50%")])));
    }

    [Fact]
    public void BR11_TC15_sechs_Fonds_werden_abgelehnt()
    {
        var result = Validate(Input(allocations:
        [
            ("INS-01", "20"), ("INS-02", "20"), ("INS-03", "20"),
            ("INS-04", "20"), ("INS-05", "10"), ("INS-06", "10"),
        ]));

        AssertViolations(result, RuleIds.AllocationCount);
        Assert.Equal(RuleMessages.AllocationCount, Assert.Single(result.Issues).Message);
    }

    [Fact]
    public void BR11_fuenf_Fonds_sind_zulaessig()
    {
        AssertValid(Validate(Input(allocations:
        [
            ("INS-01", "20"), ("INS-02", "20"), ("INS-03", "20"), ("INS-04", "20"), ("INS-05", "20"),
        ])));
    }

    [Fact]
    public void BR11_Sparplan_ohne_Fonds_wird_abgelehnt()
    {
        var input = Input() with { Allocations = [] };

        var result = Validate(input);

        // Ohne Fonds ist auch die Summe 0 % – beide Hinweise sind fachlich zutreffend.
        AssertViolations(result, RuleIds.AllocationCount, RuleIds.AllocationSum);
    }

    [Fact]
    public void BR12_TC16_doppelter_Fonds_wird_abgelehnt()
    {
        var result = Validate(Input(allocations: [("INS-01", "50"), ("INS-01", "50")]));

        AssertViolations(result, RuleIds.NoDuplicateInstrument);
        Assert.Equal(RuleMessages.NoDuplicateInstrument, Assert.Single(result.Issues).Message);
    }

    [Fact]
    public void BR13_TC17_nicht_sparplanfaehiger_Fonds_wird_mit_Namen_abgelehnt()
    {
        var result = Validate(Input(allocations: [("INS-01", "60"), ("INS-07", "40")]));

        AssertViolations(result, RuleIds.EligibleInstrument);
        var issue = Assert.Single(result.Issues);
        Assert.Equal(InputFields.AllocationInstrument(1), issue.Field);
        Assert.Equal("Der Fonds ‚Demo Immobilienfonds‘ ist derzeit nicht sparplanfähig.", issue.Message);
    }

    [Theory]
    [InlineData("INS-99")]
    [InlineData("")]
    [InlineData(null)]
    public void BR13_unbekannter_Fonds_wird_abgelehnt(string? instrumentId)
    {
        var result = Validate(Input(allocations: [("INS-01", "60"), (instrumentId, "40")]));

        AssertViolations(result, RuleIds.EligibleInstrument);
        Assert.Equal(RuleMessages.InstrumentUnknown, Assert.Single(result.Issues).Message);
    }
}
