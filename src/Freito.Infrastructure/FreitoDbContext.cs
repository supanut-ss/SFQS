using Freito.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Freito.Infrastructure;

public sealed class FreitoDbContext : DbContext
{
    public FreitoDbContext(DbContextOptions<FreitoDbContext> options) : base(options) { }

    public DbSet<Port> Ports => Set<Port>();
    public DbSet<Carrier> Carriers => Set<Carrier>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<Incoterm> Incoterms => Set<Incoterm>();
    public DbSet<IncotermChargeRule> IncotermChargeRules => Set<IncotermChargeRule>();
    public DbSet<CargoType> CargoTypes => Set<CargoType>();
    public DbSet<User> Users => Set<User>();
    public DbSet<FreightRate> FreightRates => Set<FreightRate>();
    public DbSet<LocalCharge> LocalCharges => Set<LocalCharge>();
    public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<QuotationLine> QuotationLines => Set<QuotationLine>();
    public DbSet<QuotationStatusHistory> QuotationStatusHistory => Set<QuotationStatusHistory>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FreitoDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
