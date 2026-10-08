using FundFlow.Domain.Model;
using FundFlow.Infrastructure.Orders;
using FundFlow.Infrastructure.Persistence;
using FundFlow.Infrastructure.Sessions;
using FundFlow.Web.Demo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FundFlow.Web.Pages.Sparplan;

public class IndexModel(ChangeRequestService orders, DemoSessionService sessions, FundFlowDbContext db) : DemoPageModel
{
    public SavingsPlanState State { get; private set; } = null!;
    public Portfolio Portfolio { get; private set; } = null!;
    public Customer Customer { get; private set; } = null!;

    /// <summary>Alle Versionen, neueste zuerst – auch verworfene, denn es wird nichts gelöscht.</summary>
    public IReadOnlyList<SavingsPlanVersion> Versions { get; private set; } = [];

    /// <summary>Auftragsnummer je erzeugter Version.</summary>
    public IReadOnlyDictionary<int, string> RequestNumberByVersion { get; private set; } = new Dictionary<int, string>();

    [TempData]
    public string? Notice { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var planId = await orders.GetDemoPlanIdAsync(cancellationToken);
        State = await orders.LoadStateAsync(planId, cancellationToken);
        Portfolio = await db.Portfolios.SingleAsync(p => p.Id == State.Plan.PortfolioId, cancellationToken);
        Customer = await db.Customers.SingleAsync(c => c.Id == Portfolio.CustomerId, cancellationToken);
        Versions = State.Plan.Versions.OrderByDescending(v => v.VersionNo).ToList();
        RequestNumberByVersion = State.Plan.ChangeRequests.ToDictionary(r => r.ResultingVersionId, r => r.RequestNumber);
    }

    public async Task<IActionResult> OnPostResetAsync(CancellationToken cancellationToken)
    {
        await sessions.ResetAsync(cancellationToken);
        Notice = "Die Demo wurde auf den Ausgangsstand zurückgesetzt.";
        return RedirectToPage();
    }
}
