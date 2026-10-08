using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FundFlow.Tests.Web;

public class SmokeTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Startseite_ist_erreichbar_und_zeigt_Hinweis_auf_fiktive_Daten()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Keine Anlageberatung", html);
    }
}
