using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using FundFlow.Infrastructure;
using FundFlow.Infrastructure.Persistence;
using FundFlow.Infrastructure.Sessions;
using FundFlow.Web.Demo;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = ResolveDataSource(
    builder.Configuration.GetConnectionString("FundFlow")
        ?? throw new InvalidOperationException("Verbindungszeichenfolge „FundFlow“ fehlt."),
    builder.Environment.ContentRootPath);

builder.Services.AddFundFlowInfrastructure(connectionString);

// Schlüssel für Formularschutz und TempData dauerhaft ablegen, damit Formulare einen Neustart
// des Containers überstehen (im Container: /data/keys, siehe Dockerfile).
if (builder.Configuration["DataProtection:KeysPath"] is { Length: > 0 } keysPath)
{
    builder.Services.AddDataProtection()
        .SetApplicationName("FundFlow")
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IDemoSessionAccessor, HttpDemoSessionAccessor>();
builder.Services.AddSingleton<TestResultsCache>();
builder.Services.AddHostedService<InactiveSessionCleanup>();
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddRazorPages();

// Formularschutz- und TempData-Cookie über HTTPS nur verschlüsselt senden. Beim Antiforgery-Cookie
// setzt ASP.NET das Merkmal „Secure“ sonst nicht (gefunden durch den Test „Hinter_dem_Proxy…“).
builder.Services.AddAntiforgery(options => options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest);
builder.Services.Configure<CookieTempDataProviderOptions>(options => options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest);

// Umlaute und € unverändert ausgeben statt als &#x…;-Zeichencodes.
builder.Services.Configure<Microsoft.Extensions.WebEncoders.WebEncoderOptions>(options =>
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<FundFlowDbContext>>();
    using var db = new FundFlowDbContext(options, new FixedDemoSessionAccessor(Guid.Empty));
    db.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

// HTTPS, HSTS und Sicherheits-Header übernimmt Caddy (deploy/Caddyfile.fundflow). Damit die Anwendung
// HTTPS hinter dem Proxy erkennt, setzt das Dockerfile ASPNETCORE_FORWARDEDHEADERS_ENABLED=true.

var german = new CultureInfo("de-DE");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(german),
    SupportedCultures = [german],
    SupportedUICultures = [german],
});

app.UseRouting();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

// Relative Pfade wie „App_Data/fundflow.db“ beziehen sich auf das Projektverzeichnis (nicht auf das
// Arbeitsverzeichnis des Prozesses); der Ordner wird bei Bedarf angelegt.
static string ResolveDataSource(string connectionString, string contentRoot)
{
    var csb = new SqliteConnectionStringBuilder(connectionString);
    var dataSource = csb.DataSource;
    if (string.IsNullOrEmpty(dataSource) || dataSource == ":memory:" || dataSource.StartsWith("file:", StringComparison.Ordinal))
    {
        return connectionString;
    }

    csb.DataSource = Path.GetFullPath(dataSource, contentRoot);
    Directory.CreateDirectory(Path.GetDirectoryName(csb.DataSource)!);
    return csb.ToString();
}

// Für Integrationstests mit WebApplicationFactory sichtbar machen.
public partial class Program;
