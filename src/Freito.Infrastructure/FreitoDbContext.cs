using Freito.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Freito.Infrastructure;

/// <summary>
/// M0 scope: just enough to prove the EF Core + Pomelo + MySQL pipeline works end
/// to end on the target host. Real schema (FreightRate, LocalCharge, Quotation, ...)
/// is M1 work — see technical-plan.md §2.
/// </summary>
public class FreitoDbContext : DbContext
{
    public FreitoDbContext(DbContextOptions<FreitoDbContext> options) : base(options) { }

    public DbSet<Incoterm> Incoterms => Set<Incoterm>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Incoterm>(e =>
        {
            e.HasKey(x => x.Code);
            e.Property(x => x.Code).HasMaxLength(3);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.RiskTransferPoint).HasMaxLength(200).IsRequired();
        });
    }
}
