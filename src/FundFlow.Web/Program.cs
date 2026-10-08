var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

// HTTPS und HSTS übernimmt später der Reverse Proxy auf dem VPS (Etappe 8).

app.UseRouting();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

// Für Integrationstests mit WebApplicationFactory sichtbar machen.
public partial class Program;
