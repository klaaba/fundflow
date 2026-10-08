using FundFlow.Infrastructure.Sessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FundFlow.Infrastructure.Persistence;

/// <summary>Nur für <c>dotnet ef migrations</c>: erzeugt den Kontext ohne Webanwendung.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FundFlowDbContext>
{
    public FundFlowDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<FundFlowDbContext>()
            .UseSqlite("Data Source=fundflow-design.db")
            .Options;

        return new FundFlowDbContext(options, new FixedDemoSessionAccessor(Guid.Empty));
    }
}
