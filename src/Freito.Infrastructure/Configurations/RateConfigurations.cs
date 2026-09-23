using Freito.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Freito.Infrastructure.Configurations;

internal sealed class FreightRateConfiguration : IEntityTypeConfiguration<FreightRate>
{
    public void Configure(EntityTypeBuilder<FreightRate> builder)
    {
        builder.ToTable("freight_rates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Mode).HasConversion<string>().HasMaxLength(12).IsRequired();
        builder.Property(x => x.Direction).HasConversion<string>().HasMaxLength(8).IsRequired();
        builder.Property(x => x.ContainerSize).HasMaxLength(16);
        builder.Property(x => x.WeightBreakMin).HasPrecision(12, 3);
        builder.Property(x => x.WeightBreakMax).HasPrecision(12, 3);
        builder.Property(x => x.PriceMin).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.PriceMax).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(x => x.ValidFrom).IsRequired();
        builder.Property(x => x.ValidTo).IsRequired();

        builder.HasOne<Port>().WithMany().HasForeignKey(x => x.OriginPortId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Port>().WithMany().HasForeignKey(x => x.DestinationPortId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Carrier>().WithMany().HasForeignKey(x => x.CarrierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyCode).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.OriginPortId, x.DestinationPortId, x.Mode, x.Direction, x.IsActive, x.ValidFrom, x.ValidTo });
        builder.HasIndex(x => x.CarrierId);
    }
}

internal sealed class LocalChargeConfiguration : IEntityTypeConfiguration<LocalCharge>
{
    public void Configure(EntityTypeBuilder<LocalCharge> builder)
    {
        builder.ToTable("local_charges");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Direction).HasConversion<string>().HasMaxLength(8).IsRequired();
        builder.Property(x => x.Mode).HasConversion<string>().HasMaxLength(12).IsRequired();
        builder.Property(x => x.ChargeType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CalcBasis).HasConversion<string>().HasMaxLength(24).IsRequired();
        builder.Property(x => x.AmountMin).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.AmountMax).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.MinimumCharge).HasPrecision(18, 4);
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(x => x.ChargeSide).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.HasOne<Port>().WithMany().HasForeignKey(x => x.PortId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyCode).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.PortId, x.Direction, x.Mode, x.ChargeSide, x.CalcBasis, x.ChargeType, x.CurrencyCode }).IsUnique();
        builder.HasIndex(x => x.ChargeType);
    }
}

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Entity).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ChangedAt).IsRequired();
        builder.Property(x => x.BeforeJson).HasColumnType("json");
        builder.Property(x => x.AfterJson).HasColumnType("json");
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.ChangedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.Entity, x.EntityId, x.ChangedAt });
        builder.HasIndex(x => new { x.ChangedByUserId, x.ChangedAt });
    }
}
