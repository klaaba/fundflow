using FundFlow.Domain.Model;
using FundFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FundFlow.Infrastructure.Orders;

/// <summary>Auftrag mit den Verweisen, die Auftragsübersicht und Detailansicht brauchen (US-06).</summary>
public sealed record OrderView(ChangeRequest Request, string? ReplacesNumber, string? ReplacedByNumber);

/// <summary>Lesende Abfragen für Operations: Auftragsübersicht und Auftragsdetail.</summary>
public sealed class OrderQueries(FundFlowDbContext db)
{
    public async Task<IReadOnlyList<OrderView>> ListAsync(CancellationToken cancellationToken = default)
    {
        var requests = await db.ChangeRequests
            .AsNoTracking()
            .OrderByDescending(r => r.Id)
            .ToListAsync(cancellationToken);

        return requests.Select(r => ToView(r, requests)).ToList();
    }

    public async Task<OrderView?> GetAsync(string requestNumber, CancellationToken cancellationToken = default)
    {
        var requests = await db.ChangeRequests
            .AsNoTracking()
            .Include(r => r.ChangeLog)
            .Include(r => r.ResultingVersion)
            .ToListAsync(cancellationToken);

        // Adressen werden kleingeschrieben ausgegeben (/auftraege/cr-2026-000001).
        var request = requests.SingleOrDefault(r => string.Equals(r.RequestNumber, requestNumber, StringComparison.OrdinalIgnoreCase));
        return request is null ? null : ToView(request, requests);
    }

    private static OrderView ToView(ChangeRequest request, IReadOnlyList<ChangeRequest> all) => new(
        request,
        all.SingleOrDefault(r => r.Id == request.ReplacesRequestId)?.RequestNumber,
        all.SingleOrDefault(r => r.ReplacesRequestId == request.Id)?.RequestNumber);
}
