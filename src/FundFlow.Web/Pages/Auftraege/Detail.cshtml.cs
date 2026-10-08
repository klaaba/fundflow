using FundFlow.Infrastructure.Orders;
using FundFlow.Web.Demo;
using Microsoft.AspNetCore.Mvc;

namespace FundFlow.Web.Pages.Auftraege;

/// <summary>Bestätigung nach dem Absenden (Prozessschritt 8) und Auftragsdetail für Operations (US-06).</summary>
public class DetailModel(OrderQueries queries) : DemoPageModel
{
    public const string CreatedKey = "OrderCreated";
    public const string ShiftHintKey = "OrderShiftHint";

    public OrderView Order { get; private set; } = null!;
    public bool JustCreated { get; private set; }
    public string? ShiftHint { get; private set; }

    public async Task<IActionResult> OnGetAsync(string number, CancellationToken cancellationToken)
    {
        var order = await queries.GetAsync(number, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        Order = order;
        JustCreated = TempData[CreatedKey] is true;
        ShiftHint = TempData[ShiftHintKey] as string;
        return Page();
    }
}
