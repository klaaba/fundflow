using FundFlow.Infrastructure.Sessions;

namespace FundFlow.Web.Demo;

/// <summary>
/// Demo-Sitzung aus einem technisch notwendigen Cookie (Fachkonzept, Abschnitt 14).
/// Fehlt das Cookie oder ist es ungültig, wird eine neue Sitzung vergeben.
/// </summary>
public sealed class HttpDemoSessionAccessor(IHttpContextAccessor httpContextAccessor) : IDemoSessionAccessor
{
    public const string CookieName = "fundflow_demo";
    private const string ItemKey = "FundFlow.DemoSessionId";

    public Guid SessionId
    {
        get
        {
            var context = httpContextAccessor.HttpContext
                ?? throw new InvalidOperationException("Demo-Sitzung ist nur innerhalb einer Anfrage verfügbar.");

            if (context.Items[ItemKey] is Guid known)
            {
                return known;
            }

            if (!Guid.TryParse(context.Request.Cookies[CookieName], out var sessionId) || sessionId == Guid.Empty)
            {
                sessionId = Guid.NewGuid();
                context.Response.Cookies.Append(CookieName, sessionId.ToString(), new CookieOptions
                {
                    HttpOnly = true,
                    Secure = context.Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    IsEssential = true,
                });
            }

            context.Items[ItemKey] = sessionId;
            return sessionId;
        }
    }
}
