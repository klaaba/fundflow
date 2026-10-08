using FundFlow.Infrastructure.Orders;
using FundFlow.Infrastructure.Persistence;
using FundFlow.Infrastructure.Sessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FundFlow.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>Datenhaltung und Auftragsverarbeitung. <see cref="IDemoSessionAccessor"/> registriert die Webanwendung.</summary>
    public static IServiceCollection AddFundFlowInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<FundFlowDbContext>(options => options.UseSqlite(connectionString));
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ChangeRequestService>();
        services.AddScoped<DemoSessionService>();
        services.AddScoped<OrderQueries>();
        return services;
    }
}
