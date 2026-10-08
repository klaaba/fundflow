using FundFlow.Domain.Model;
using FundFlow.Domain.Rules;
using static FundFlow.Tests.Domain.ValidationTestData;

namespace FundFlow.Tests.Domain;

/// <summary>Regelübergreifendes Verhalten: BR-15, gemeinsame Fehlermeldung (E-10), Ergebnis bei Erfolg.</summary>
public class ChangeRequestValidatorTests
{
    [Fact]
    public void BR15_TC24_unveraenderte_Eingabe_wird_abgelehnt()
    {
        var result = Validate(Input());

        AssertViolations(result, RuleIds.MustDiffer);
        var issue = Assert.Single(result.Issues);
        Assert.Equal(InputFields.Form, issue.Field);
        Assert.Equal(RuleMessages.SameAsCurrentVersion, issue.Message);
    }

    [Fact]
    public void BR15_geaenderte_Reihenfolge_der_Fonds_ist_keine_Aenderung()
    {
        var result = Validate(Input(allocations: [("INS-02", "40"), ("INS-01", "60")]));

        AssertViolations(result, RuleIds.MustDiffer);
    }

    [Fact]
    public void BR15_geaendertes_Wunschdatum_allein_ist_keine_Aenderung()
    {
        // BR-15 vergleicht nur Sparrate, Ausführungstag und Aufteilung.
        var result = Validate(Input(requestedFrom: "2026-11-01"));

        AssertViolations(result, RuleIds.MustDiffer);
    }

    [Fact]
    public void BR15_TC28_Eingabe_gleich_offenem_Auftrag_wird_als_Doppelung_abgelehnt()
    {
        var result = Validate(Input(amount: "250,00"), ContextA1());

        AssertViolations(result, RuleIds.MustDiffer);
        Assert.Equal(
            "Diese Änderung liegt bereits als offener Auftrag CR-2026-000001 vor.",
            Assert.Single(result.Issues).Message);
    }

    [Fact]
    public void BR15_TC29_Eingabe_gleich_gueltiger_Version_wird_auch_bei_offenem_Auftrag_abgelehnt()
    {
        var result = Validate(Input(amount: "150,00"), ContextA1());

        AssertViolations(result, RuleIds.MustDiffer);
        Assert.Equal(RuleMessages.SameAsCurrentVersion, Assert.Single(result.Issues).Message);
    }

    [Fact]
    public void BR15_abweichende_Eingabe_ist_bei_offenem_Auftrag_zulaessig()
    {
        // Grundlage für TC-25 (Ersetzung, Etappe 3)
        AssertValid(Validate(Input(amount: "300,00"), ContextA1()));
    }

    [Fact]
    public void TC27_mehrere_Verstoesse_werden_gemeinsam_gemeldet()
    {
        var result = Validate(Input(
            amount: "20,00",
            allocations: [("INS-01", "60"), ("INS-02", "30"), ("INS-03", "8")]));

        AssertViolations(result, RuleIds.MinMonthlyAmount, RuleIds.AllocationSum);
        Assert.Equal(2, result.Issues.Count);
    }

    [Fact]
    public void Alle_Felder_fehlerhaft_ergibt_je_Feld_eine_Meldung()
    {
        var input = new ChangeRequestInput("abc", "7", "gestern", [new AllocationInput("INS-07", "100")]);

        var result = Validate(input);

        AssertViolations(result, RuleIds.InputFormat, RuleIds.ExecutionDay, RuleIds.EligibleInstrument);
        Assert.Equal(
            [InputFields.MonthlyAmount, InputFields.ExecutionDay, InputFields.RequestedFrom, InputFields.AllocationInstrument(0)],
            result.Issues.Select(i => i.Field));
    }

    [Fact]
    public void TC07_gueltige_Aenderung_liefert_die_geprueften_Fachwerte()
    {
        var result = Validate(Input(
            amount: "200,00",
            day: "1",
            allocations: [("INS-01", "50"), ("INS-02", "50")]));

        AssertValid(result);
        var change = result.Change!;
        Assert.Equal(200.00m, change.Terms.MonthlyAmount);
        Assert.Equal(1, change.Terms.ExecutionDay);
        Assert.Equal([new AllocationLine("INS-01", 50), new AllocationLine("INS-02", 50)], change.Terms.Allocations);
        Assert.Equal(ReferenceDate, change.RequestedFrom);
    }
}
