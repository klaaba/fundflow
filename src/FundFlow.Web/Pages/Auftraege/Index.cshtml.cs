using FundFlow.Infrastructure.Orders;
using FundFlow.Web.Demo;

namespace FundFlow.Web.Pages.Auftraege;

/// <summary>Auftragsübersicht für Operations (US-06, Kriterium 1).</summary>
public class IndexModel(OrderQueries queries) : DemoPageModel
{
    public IReadOnlyList<OrderView> Orders { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Orders = await queries.ListAsync(cancellationToken);
}
