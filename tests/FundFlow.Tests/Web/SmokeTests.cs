using System.Net;

namespace FundFlow.Tests.Web;

public sealed class SmokeTests(FundFlowWebFactory factory) : IClassFixture<FundFlowWebFactory>
{
    [Theory]
    [InlineData("/", "Keine Anlageberatung")]
    [InlineData("/sparplan", "Sparplan SP-000001")]
    [InlineData("/sparplan/aendern", "Sparplan ändern")]
    [InlineData("/auftraege", "Noch keine Aufträge")]
    [InlineData("/testansicht", "30</span> von 30 Testfällen erfüllt")]
    [InlineData("/ueber", "Über das Projekt")]
    [InlineData("/impressum", "Angaben gemäß § 5 DDG")]
    [InlineData("/datenschutz", "fundflow_demo")]
    public async Task Seite_ist_erreichbar(string url, string expectedText)
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(url);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(expectedText, html);
    }

    [Fact]
    public async Task Unbekannter_Auftrag_ergibt_404()
    {
        var response = await factory.CreateClient().GetAsync("/auftraege/CR-2026-999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Demo_Sitzung_wird_per_Cookie_vergeben()
    {
        var client = factory.CreateClient(new() { HandleCookies = false });

        var response = await client.GetAsync("/sparplan");

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith("fundflow_demo=", StringComparison.Ordinal));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
    }
}
