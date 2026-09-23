using Freito.Domain.Entities;
using Freito.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Freito.Infrastructure.Configurations;

internal sealed class PortConfiguration : IEntityTypeConfiguration<Port>
{
    public void Configure(EntityTypeBuilder<Port> builder)
    {
        builder.ToTable("ports");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(8).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
        builder.Property(x => x.City).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Country).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(8).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => new { x.Type, x.Country, x.City });
    }
}

internal sealed class CarrierConfiguration : IEntityTypeConfiguration<Carrier>
{
    public void Configure(EntityTypeBuilder<Carrier> builder)
    {
        builder.ToTable("carriers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(16).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

internal sealed class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("currencies");
        builder.HasKey(x => x.Code);
        builder.Property(x => x.Code).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.DecimalDigits).IsRequired();
        builder.HasData(
            new Currency { Code = "USD", Name = "US Dollar", DecimalDigits = 2 },
            new Currency { Code = "THB", Name = "Thai Baht", DecimalDigits = 2 },
            new Currency { Code = "EUR", Name = "Euro", DecimalDigits = 2 });
    }
}

internal sealed class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> builder)
    {
        builder.ToTable("exchange_rates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(x => x.RateToBase).HasPrecision(18, 8).IsRequired();
        builder.Property(x => x.EffectiveDate).IsRequired();
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyCode).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.CurrencyCode, x.EffectiveDate }).IsUnique();
        builder.HasData(new ExchangeRate
        {
            Id = 1,
            CurrencyCode = "USD",
            RateToBase = 1m,
            EffectiveDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });
    }
}

internal sealed class IncotermConfiguration : IEntityTypeConfiguration<Incoterm>
{
    public void Configure(EntityTypeBuilder<Incoterm> builder)
    {
        builder.ToTable("incoterms");
        builder.HasKey(x => x.Code);
        builder.Property(x => x.Code).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.RiskTransferPoint).HasMaxLength(240).IsRequired();
        builder.HasData(IncotermSeeds.Terms);
    }
}

internal sealed class IncotermChargeRuleConfiguration : IEntityTypeConfiguration<IncotermChargeRule>
{
    public void Configure(EntityTypeBuilder<IncotermChargeRule> builder)
    {
        builder.ToTable("incoterm_charge_rules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.IncotermCode).HasMaxLength(3).IsRequired();
        builder.Property(x => x.ChargeSide).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(x => x.Payer).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.HasOne<Incoterm>().WithMany().HasForeignKey(x => x.IncotermCode).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.IncotermCode, x.ChargeSide }).IsUnique();
        builder.HasData(IncotermSeeds.ChargeRules);
    }
}

internal sealed class CargoTypeConfiguration : IEntityTypeConfiguration<CargoType>
{
    public void Configure(EntityTypeBuilder<CargoType> builder)
    {
        builder.ToTable("cargo_types");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}

internal static class IncotermSeeds
{
    // Risk points follow ICC Incoterms 2020 rules; payer mappings follow the Operation-approved worksheet.
    public static readonly Incoterm[] Terms =
    [
        new() { Code = "EXW", Name = "Ex Works", RiskTransferPoint = "Seller's named premises; goods placed at the buyer's disposal, not loaded.", SellerPaysFreight = false },
        new() { Code = "FCA", Name = "Free Carrier", RiskTransferPoint = "Named place after delivery to the buyer-nominated carrier.", SellerPaysFreight = false },
        new() { Code = "FAS", Name = "Free Alongside Ship", RiskTransferPoint = "Alongside the vessel at the named port of shipment.", SellerPaysFreight = false },
        new() { Code = "FOB", Name = "Free on Board", RiskTransferPoint = "On board the vessel at the named port of shipment.", SellerPaysFreight = false },
        new() { Code = "CFR", Name = "Cost and Freight", RiskTransferPoint = "On board the vessel at the port of shipment.", SellerPaysFreight = true },
        new() { Code = "CIF", Name = "Cost, Insurance and Freight", RiskTransferPoint = "On board the vessel at the port of shipment.", SellerPaysFreight = true },
        new() { Code = "CPT", Name = "Carriage Paid To", RiskTransferPoint = "When the goods are handed over to the seller-contracted carrier.", SellerPaysFreight = true },
        new() { Code = "CIP", Name = "Carriage and Insurance Paid To", RiskTransferPoint = "When the goods are handed over to the seller-contracted carrier.", SellerPaysFreight = true },
        new() { Code = "DPU", Name = "Delivered at Place Unloaded", RiskTransferPoint = "At the named destination after the goods have been unloaded.", SellerPaysFreight = true },
        new() { Code = "DAP", Name = "Delivered at Place", RiskTransferPoint = "At the named destination, ready for unloading.", SellerPaysFreight = true },
        new() { Code = "DDP", Name = "Delivered Duty Paid", RiskTransferPoint = "At the named destination, cleared for import and ready for unloading.", SellerPaysFreight = true },
    ];

    public static readonly IncotermChargeRule[] ChargeRules = BuildChargeRules();

    private static IncotermChargeRule[] BuildChargeRules()
    {
        var rows = new List<IncotermChargeRule>(22);
        var id = 1;

        foreach (var term in Terms)
        {
            var (origin, destination) = term.Code switch
            {
                "EXW" => (Payer.Buyer, Payer.Buyer),
                "DPU" or "DAP" or "DDP" => (Payer.Seller, Payer.Seller),
                _ => (Payer.Seller, Payer.Buyer),
            };

            rows.Add(new IncotermChargeRule { Id = id++, IncotermCode = term.Code, ChargeSide = ChargeSide.Origin, Payer = origin });
            rows.Add(new IncotermChargeRule { Id = id++, IncotermCode = term.Code, ChargeSide = ChargeSide.Destination, Payer = destination });
        }

        return rows.ToArray();
    }
}
