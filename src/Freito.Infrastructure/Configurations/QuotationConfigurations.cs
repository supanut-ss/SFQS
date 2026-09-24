using Freito.Domain.Entities;
using Freito.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Freito.Infrastructure.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Email).HasMaxLength(254).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();

        // Bootstrap Admin so the very first login is possible without direct DB access — there's
        // no self-signup (User doc comment) and Admin is the only role that can create more users
        // (MasterDataController). Password is "ChangeMe123!"; hash computed once with
        // PasswordHasher.HashWithSalt for a reproducible migration, never used for a real user's
        // password. MUST be changed immediately after first login — see README.md.
        builder.HasData(new User
        {
            Id = 1,
            Email = "admin@freito.local",
            PasswordHash = "100000.RnJlaXRvQm9vdHN0cmFwU2FsdA==.ARqOilZbMKbjVo4Xi+DXpTkl2AQYWlJcX+e3XKjy7VA=",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });
    }
}

internal sealed class QuotationConfiguration : IEntityTypeConfiguration<Quotation>
{
    public void Configure(EntityTypeBuilder<Quotation> builder)
    {
        builder.ToTable("quotations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.QuoteNo).HasMaxLength(32).IsRequired();
        builder.HasIndex(x => x.QuoteNo).IsUnique();
        builder.Property(x => x.CustomerName).HasMaxLength(160).IsRequired();
        builder.Property(x => x.CustomerCompany).HasMaxLength(160);
        builder.Property(x => x.CustomerEmail).HasMaxLength(254).IsRequired();
        builder.Property(x => x.CustomerPhone).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Direction).HasConversion<string>().HasMaxLength(8).IsRequired();
        builder.Property(x => x.Mode).HasConversion<string>().HasMaxLength(12).IsRequired();
        builder.Property(x => x.ContainerSize).HasMaxLength(16);
        builder.Property(x => x.Cbm).HasPrecision(12, 3);
        builder.Property(x => x.WeightKg).HasPrecision(12, 3);
        builder.Property(x => x.IncotermCode).HasMaxLength(3).IsRequired();
        builder.Property(x => x.QuoteCurrency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.FxRateUsed).HasPrecision(18, 8).IsRequired();
        builder.Property(x => x.RateSource).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(x => x.FreightCost).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.LocalChargeTotal).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.Subtotal).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.FinalPrice).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
        builder.Property(x => x.ReadyDate).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.TransitTime).HasMaxLength(100);
        builder.Property(x => x.Frequency).HasMaxLength(100);
        builder.Property(x => x.ClosingSchedule).HasMaxLength(200);
        builder.Property(x => x.CarrierInfo).HasMaxLength(150);
        builder.Property(x => x.PaymentTerms).HasMaxLength(150);
        builder.Property(x => x.InsuranceStatus).HasMaxLength(100);
        builder.Property(x => x.TermsAndConditions).HasMaxLength(4000);
        builder.Property(x => x.DimensionsJson).HasMaxLength(4000);
        builder.HasOne<Port>().WithMany().HasForeignKey(x => x.OriginPortId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Port>().WithMany().HasForeignKey(x => x.DestinationPortId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CargoType>().WithMany().HasForeignKey(x => x.CargoTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Incoterm>().WithMany().HasForeignKey(x => x.IncotermCode).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.QuoteCurrency).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.ApprovedByUserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(x => new { x.Status, x.CreatedByUserId });
        builder.HasIndex(x => x.ReadyDate);
    }
}

internal sealed class QuotationLineConfiguration : IEntityTypeConfiguration<QuotationLine>
{
    public void Configure(EntityTypeBuilder<QuotationLine> builder)
    {
        builder.ToTable("quotation_lines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Basis).HasMaxLength(80).IsRequired();
        builder.Property(x => x.UnitPrice).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.Qty).HasPrecision(12, 3).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.HasOne<Quotation>().WithMany().HasForeignKey(x => x.QuotationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<FreightRate>().WithMany().HasForeignKey(x => x.SourceRateId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<LocalCharge>().WithMany().HasForeignKey(x => x.SourceLocalChargeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.QuotationId);
    }
}

internal sealed class QuotationStatusHistoryConfiguration : IEntityTypeConfiguration<QuotationStatusHistory>
{
    public void Configure(EntityTypeBuilder<QuotationStatusHistory> builder)
    {
        builder.ToTable("quotation_status_history");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(24).IsRequired();
        builder.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(24).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(1000);
        builder.Property(x => x.At).IsRequired();
        builder.HasOne<Quotation>().WithMany().HasForeignKey(x => x.QuotationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(x => new { x.QuotationId, x.At });
    }
}
