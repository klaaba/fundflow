using System.Net;
using System.Text.RegularExpressions;
using FundFlow.Scenarios;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace FundFlow.Tests.Web;

/// <summary>
/// Webanwendung mit eigener SQLite-Datenbank im Arbeitsspeicher und festem Datum 08.10.2026,
/// damit Wirksamkeitstermine vorhersagbar sind.
/// </summary>
public sealed class FundFlowWebFactory : WebApplicationFactory<Program>
{
    public static readonly DateOnly Today = new(2026, 10, 8);

    private readonly string _connectionString = $"Data Source=file:fundflow-{Guid.NewGuid():N}?mode=memory&cache=shared";
    private readonly SqliteConnection _keepAlive;

    public FundFlowWebFactory()
    {
        // Eine gemeinsame In-Memory-Datenbank lebt nur, solange eine Verbindung offen ist.
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:FundFlow", _connectionString);
        builder.ConfigureTestServices(services =>
            services.AddSingleton<TimeProvider>(FixedTimeProvider.AtBerlinNoon(Today)));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _keepAlive.Dispose();
        }
    }
}

/// <summary>Formulare wie im Browser absenden: mit Antiforgery-Token und Sitzungs-Cookie.</summary>
internal static partial class FormPoster
{
    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex TokenPattern();

    public static async Task<string> GetTokenAsync(HttpClient client, string url)
    {
        var html = await client.GetStringAsync(url);
        return TokenPattern().Match(html).Groups[1].Value;
    }

    public static async Task<(HttpStatusCode Status, string Html, Uri? Location)> PostAsync(
        HttpClient client, string url, string token, IEnumerable<KeyValuePair<string, string>> fields)
    {
        var content = new FormUrlEncodedContent(fields.Append(new("__RequestVerificationToken", token)));
        var response = await client.PostAsync(url, content);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(), response.RequestMessage?.RequestUri);
    }

    /// <summary>Formularfelder einer Sparplanänderung; ohne Angabe gelten die Werte von A0.</summary>
    public static List<KeyValuePair<string, string>> ChangeFields(
        string amount = "150,00",
        string day = "15",
        string requestedFrom = "2026-10-08",
        params (string Fund, string Percentage)[] allocations)
    {
        var rows = allocations.Length == 0 ? [("INS-01", "60"), ("INS-02", "40")] : allocations;
        var fields = new List<KeyValuePair<string, string>>
        {
            new("Form.MonthlyAmount", amount),
            new("Form.ExecutionDay", day),
            new("Form.RequestedFrom", requestedFrom),
        };
        for (var i = 0; i < rows.Length; i++)
        {
            fields.Add(new($"Form.Allocations[{i}].InstrumentId", rows[i].Fund));
            fields.Add(new($"Form.Allocations[{i}].Percentage", rows[i].Percentage));
        }

        return fields;
    }
}
