using System.Globalization;
using FundFlow.Domain.Scheduling;
using FundFlow.Infrastructure.Sessions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FundFlow.Web.Demo;

/// <summary>Basis für Seiten mit Demo-Daten: stellt vor jedem Aufruf die Sitzung mit Ausgangsstand A0 sicher.</summary>
public abstract class DemoPageModel : PageModel
{
    protected static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    public DateOnly Today { get; private set; }

    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var services = context.HttpContext.RequestServices;
        await services.GetRequiredService<DemoSessionService>().EnsureSessionAsync(context.HttpContext.RequestAborted);
        Today = BusinessCalendar.Today(services.GetRequiredService<TimeProvider>());

        await next();
    }
}
