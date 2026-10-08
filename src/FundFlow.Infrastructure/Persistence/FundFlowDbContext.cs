using FundFlow.Domain.Model;
using FundFlow.Infrastructure.Seed;
using FundFlow.Infrastructure.Sessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FundFlow.Infrastructure.Persistence;

/// <summary>
/// Gemeinsame SQLite-Datenbank für alle Demo-Sitzungen (E-11). Sitzungsbezogene Tabellen tragen
/// eine Spalte <c>DemoSessionId</c>, die nur hier existiert (Schatteneigenschaft) – das fachliche
/// Modell kennt sie nicht. Jede Abfrage wird automatisch auf die Sitzung der aktuellen Anfrage gefiltert.
/// </summary>
public class FundFlowDbContext(DbContextOptions<FundFlowDbContext> options, IDemoSessionAccessor session)
    : DbContext(options)
{
    public const string SessionColumn = "DemoSessionId";

    private static readonly Type[] SessionScopedTypes =
    [
        typeof(Customer), typeof(Portfolio), typeof(SavingsPlan), typeof(SavingsPlanVersion),
        typeof(Allocation), typeof(ChangeRequest), typeof(ChangeRequestStatusHistory), typeof(ChangeLogEntry),
    ];

    /// <summary>Sitzung dieses Kontexts; wird von den Abfragefiltern als Parameter gelesen.</summary>
    public Guid SessionId { get; } = session.SessionId;

    public DbSet<DemoSession> DemoSessions => Set<DemoSession>();
    public DbSet<Instrument> Instruments => Set<Instrument>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Portfolio> Portfolios => Set<Portfolio>();
    public DbSet<SavingsPlan> SavingsPlans => Set<SavingsPlan>();
    public DbSet<SavingsPlanVersion> SavingsPlanVersions => Set<SavingsPlanVersion>();
    public DbSet<Allocation> Allocations => Set<Allocation>();
    public DbSet<ChangeRequest> ChangeRequests => Set<ChangeRequest>();
    public DbSet<ChangeRequestStatusHistory> StatusHistory => Set<ChangeRequestStatusHistory>();
    public DbSet<ChangeLogEntry> ChangeLog => Set<ChangeLogEntry>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite kann DateTimeOffset nicht sortieren oder vergleichen – daher als Zahl (UTC-Ticks) speichern.
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DemoSession>().ToTable("DemoSessions");

        modelBuilder.Entity<Instrument>(e =>
        {
            e.HasKey(i => i.InstrumentId);
            e.HasData(DemoData.Instruments);
        });

        modelBuilder.Entity<Portfolio>()
            .HasOne<Customer>().WithMany(c => c.Portfolios).HasForeignKey(p => p.CustomerId);

        modelBuilder.Entity<SavingsPlan>(e =>
        {
            e.HasOne<Portfolio>().WithMany(p => p.SavingsPlans).HasForeignKey(s => s.PortfolioId);
        });

        modelBuilder.Entity<SavingsPlanVersion>(e =>
        {
            e.HasOne<SavingsPlan>().WithMany(s => s.Versions).HasForeignKey(v => v.SavingsPlanId);
            e.HasIndex(v => new { v.SavingsPlanId, v.VersionNo }).IsUnique();
            e.Property(v => v.MonthlyAmount).HasPrecision(12, 2);
        });

        modelBuilder.Entity<Allocation>(e =>
        {
            e.HasOne<SavingsPlanVersion>().WithMany(v => v.Allocations).HasForeignKey(a => a.VersionId);
            e.HasOne<Instrument>().WithMany().HasForeignKey(a => a.InstrumentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ChangeRequest>(e =>
        {
            e.HasIndex(r => r.RequestNumber).IsUnique();
            e.Property(r => r.Status).HasConversion<string>().HasMaxLength(40);
            e.HasOne<SavingsPlan>().WithMany(s => s.ChangeRequests).HasForeignKey(r => r.SavingsPlanId);

            // Die Verbindung Auftrag → erzeugte Version liegt technisch beim Auftrag;
            // die Version erreicht ihren Auftrag über CreatedByRequest (Fachkonzept 10.1).
            e.HasOne(r => r.ResultingVersion).WithOne(v => v.CreatedByRequest)
                .HasForeignKey<ChangeRequest>(r => r.ResultingVersionId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(r => r.ReplacesRequest).WithMany()
                .HasForeignKey(r => r.ReplacesRequestId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasMany(r => r.StatusHistory).WithOne().HasForeignKey(h => h.RequestId);
            e.HasMany(r => r.ChangeLog).WithOne().HasForeignKey(l => l.RequestId);
            e.Navigation(r => r.StatusHistory).AutoInclude();
        });

        modelBuilder.Entity<ChangeRequestStatusHistory>(e =>
        {
            e.ToTable("ChangeRequestStatusHistory");
            e.Property(h => h.FromStatus).HasConversion<string>().HasMaxLength(40);
            e.Property(h => h.ToStatus).HasConversion<string>().HasMaxLength(40);
        });

        modelBuilder.Entity<ChangeLogEntry>().ToTable("ChangeLog");

        foreach (var type in SessionScopedTypes)
        {
            ConfigureSessionScope(modelBuilder, type);
        }
    }

    /// <summary>Sitzungsspalte, Fremdschlüssel mit kaskadierendem Löschen und Abfragefilter.</summary>
    private void ConfigureSessionScope(ModelBuilder modelBuilder, Type type)
    {
        var entity = modelBuilder.Entity(type);
        entity.Property<Guid>(SessionColumn);
        entity.HasIndex(SessionColumn);
        entity.HasOne(typeof(DemoSession)).WithMany().HasForeignKey(SessionColumn).OnDelete(DeleteBehavior.Cascade);

        var method = typeof(FundFlowDbContext)
            .GetMethod(nameof(ApplySessionFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(type);
        method.Invoke(this, [modelBuilder]);
    }

    private void ApplySessionFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class =>
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => EF.Property<Guid>(e, SessionColumn) == SessionId);

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AssignSession();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        AssignSession();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>Neue sitzungsbezogene Datensätze gehören immer zur Sitzung dieses Kontexts.</summary>
    private void AssignSession()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added && SessionScopedTypes.Contains(entry.Metadata.ClrType))
            {
                entry.Property(SessionColumn).CurrentValue = SessionId;
            }
        }
    }
}
