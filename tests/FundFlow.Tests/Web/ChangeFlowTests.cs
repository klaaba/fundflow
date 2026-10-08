using static FundFlow.Tests.Web.FormPoster;

namespace FundFlow.Tests.Web;

/// <summary>Der Prozess aus Fachkonzept Abschnitt 7 über die Oberfläche – so, wie ihn ein Besucher durchläuft.</summary>
public sealed class ChangeFlowTests(FundFlowWebFactory factory) : IClassFixture<FundFlowWebFactory>
{
    private const string ChangeUrl = "/sparplan/aendern";

    [Fact]
    public async Task Aenderung_pruefen_bestaetigen_und_Auftrag_ansehen()
    {
        var client = factory.CreateClient();
        var token = await GetTokenAsync(client, ChangeUrl);

        // Schritte 4 und 5: prüfen und Zusammenfassung
        var review = await PostAsync(client, $"{ChangeUrl}?handler=Check", token, ChangeFields(amount: "250,00"));
        Assert.Contains("Änderung prüfen und bestätigen", review.Html);
        Assert.Contains("15. Oktober 2026", review.Html);
        Assert.Contains("Ihr Wunschdatum: Donnerstag, 8. Oktober 2026", review.Html);
        Assert.Contains("150,00 €", review.Html);
        Assert.Contains("250,00 €", review.Html);

        // Schritte 6 bis 8: absenden, Weiterleitung zur Bestätigung
        var fields = ChangeFields(amount: "250,00");
        fields.Add(new("ConfirmedEffectiveDate", "2026-10-15"));
        var created = await PostAsync(client, $"{ChangeUrl}?handler=Submit", token, fields);

        Assert.Contains("Auftrag angelegt.", created.Html);
        Assert.Contains("fachlich geprüft", created.Html);
        Assert.Matches(@"/auftraege/cr-2026-\d{6}$", created.Location!.AbsolutePath.ToLowerInvariant());

        var plan = await client.GetStringAsync("/sparplan");
        Assert.Contains("Geplante Änderung", plan);
        Assert.Contains("Eine separate Stornierung ist in dieser Demo-Version nicht möglich.", plan);
    }

    [Fact]
    public async Task TC04_ueber_das_Formular_zeigt_Meldung_mit_Regel_und_legt_nichts_an()
    {
        var client = factory.CreateClient();
        var token = await GetTokenAsync(client, ChangeUrl);

        var result = await PostAsync(client, $"{ChangeUrl}?handler=Check", token,
            ChangeFields(allocations: [("INS-01", "60"), ("INS-02", "30"), ("INS-03", "8")]));

        Assert.Contains("Der Auftrag wurde nicht angelegt", result.Html);
        Assert.Contains("ergibt aktuell 98 %", result.Html);
        Assert.Contains("BR-04", result.Html);
        Assert.Contains("Noch keine Aufträge", await client.GetStringAsync("/auftraege"));
    }

    [Fact]
    public async Task DEF001_direkt_abgesendetes_Formular_mit_98_Prozent_wird_abgelehnt()
    {
        // Ohne vorherige Zusammenfassung – wie ein manipuliertes Formular oder ein direkter Aufruf.
        var client = factory.CreateClient();
        var token = await GetTokenAsync(client, ChangeUrl);
        var fields = ChangeFields(allocations: [("INS-01", "60"), ("INS-02", "30"), ("INS-03", "8")]);
        fields.Add(new("ConfirmedEffectiveDate", "2026-10-15"));

        var result = await PostAsync(client, $"{ChangeUrl}?handler=Submit", token, fields);

        Assert.Contains("Der Auftrag wurde nicht angelegt", result.Html);
        Assert.Contains("ergibt aktuell 98 %", result.Html);
        Assert.Contains("Noch keine Aufträge", await client.GetStringAsync("/auftraege"));
    }

    [Fact]
    public async Task Abweichender_Termin_beim_Absenden_zeigt_die_Zusammenfassung_erneut()
    {
        var client = factory.CreateClient();
        var token = await GetTokenAsync(client, ChangeUrl);
        var fields = ChangeFields(amount: "275,00");
        fields.Add(new("ConfirmedEffectiveDate", "2026-10-01")); // veralteter Termin aus einer früheren Zusammenfassung

        var result = await PostAsync(client, $"{ChangeUrl}?handler=Submit", token, fields);

        Assert.Contains("Der Wirksamkeitstermin hat sich seit Ihrer letzten Prüfung geändert", result.Html);
        Assert.Contains("15. Oktober 2026", result.Html);
        Assert.Contains("Noch keine Aufträge", await client.GetStringAsync("/auftraege"));
    }

    [Fact]
    public async Task Leere_Fondszeilen_werden_ignoriert_und_Zeilen_lassen_sich_ohne_JavaScript_entfernen()
    {
        var client = factory.CreateClient();
        var token = await GetTokenAsync(client, ChangeUrl);
        var fields = ChangeFields(allocations: [("INS-01", "100"), ("", "")]);

        var review = await PostAsync(client, $"{ChangeUrl}?handler=Check", token, fields);
        Assert.Contains("Änderung prüfen und bestätigen", review.Html);

        var removed = await PostAsync(client, $"{ChangeUrl}?handler=RemoveRow", token,
            ChangeFields(allocations: [("INS-01", "60"), ("INS-02", "40")]).Append(new("index", "1")));
        Assert.DoesNotContain("name=\"Form.Allocations[1].InstrumentId\"", removed.Html);
    }

    [Fact]
    public async Task Besucher_sehen_nur_ihre_eigenen_Auftraege()
    {
        var first = factory.CreateClient();
        var second = factory.CreateClient();
        var token = await GetTokenAsync(first, ChangeUrl);
        var fields = ChangeFields(amount: "333,00");
        fields.Add(new("ConfirmedEffectiveDate", "2026-10-15"));

        var created = await PostAsync(first, $"{ChangeUrl}?handler=Submit", token, fields);
        Assert.Contains("Auftrag angelegt.", created.Html);

        Assert.Contains("Noch keine Aufträge", await second.GetStringAsync("/auftraege"));
        Assert.DoesNotContain("333,00 €", await second.GetStringAsync("/sparplan"));
    }
}
