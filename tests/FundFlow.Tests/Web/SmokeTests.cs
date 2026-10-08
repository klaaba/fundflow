using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace FundFlow.Tests.Web;

public sealed class SmokeTests(FundFlowWebFactory factory) : IClassFixture<FundFlowWebFactory>
{
    [Theory]
    [InlineData("/", "Keine Anlageberatung")]
    [InlineData("/sparplan", "Sparplan SP-000001")]
    [InlineData("/sparplan/aendern", "Donnerstag, 8. Oktober 2026")]
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
    public async Task Hinter_dem_Proxy_erhalten_Cookies_das_Merkmal_Secure()
    {
        // Wie im Container: Caddy beendet TLS und meldet das Schema per X-Forwarded-Proto.
        var behindProxy = factory.WithWebHostBuilder(b => b.UseSetting("FORWARDEDHEADERS_ENABLED", "true"));
        var client = behindProxy.CreateClient(new() { HandleCookies = false });
        var request = new HttpRequestMessage(HttpMethod.Get, "/sparplan/aendern");
        request.Headers.Add("X-Forwarded-Proto", "https");

        var response = await client.SendAsync(request);

        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        Assert.Contains(cookies, c => c.StartsWith("fundflow_demo=", StringComparison.Ordinal));
        Assert.Contains(cookies, c => c.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal));
        Assert.All(cookies, c => Assert.Contains("secure", c, StringComparison.OrdinalIgnoreCase));
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
