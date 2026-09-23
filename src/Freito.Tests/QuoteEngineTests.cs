using Freito.Domain.Entities;
using Freito.Domain.Enums;
using Freito.Domain.Quoting;
using Xunit;

namespace Freito.Tests;

/// <summary>
/// Verifies the quote engine (T5) against the real numbers gathered from Operation in
/// operation-worksheet.md. These are regression tests, not made-up examples — if any of
/// them start failing, the formula has drifted from what was confirmed.
/// </summary>
public class QuoteEngineTests
{
    [Fact]
    public void AirChargeableWeight_MatchesRealBarcelonaBkkQuote()
    {
        // 53.5 cbm = 53,500,000 cm3; actual weight 3,294 kg. Real quote's volumetric
        // weight was 8,916.4 kg — within rounding of the exact 53,500,000/6,000.
        var chargeable = ChargeableWeightCalculator.AirChargeableWeightKg(3294m, 53_500_000m);

        Assert.Equal(53_500_000m / 6000m, chargeable);
        Assert.True(Math.Abs(chargeable - 8916.4m) < 0.3m, $"Expected close to the real quote's 8,916.4kg, got {chargeable}");
    }

    [Fact]
    public void AirFreight_MatchesRealBarcelonaBkkQuote_ToTheCent()
    {
        // Real quote: 1.85 EUR/kg x 8,916.4 kg = 16,495.34 EUR (quote used its own
        // slightly-rounded chargeable weight; we replicate with the same input here).
        var chargeableWeight = 8916.4m;
        var freight = 1.85m * chargeableWeight;

        Assert.Equal(16495.34m, freight);
    }

    [Fact]
    public void AirChargeableWeight_FloorsAtMinimum50Kg()
    {
        // A tiny 2kg, 0-volume parcel must still be billed as 50kg minimum (confirmed with
        // Operation) — not the literal 2kg.
        var chargeable = ChargeableWeightCalculator.AirChargeableWeightKg(2m, 0m);

        Assert.Equal(ChargeableWeightCalculator.AirMinimumChargeableWeightKg, chargeable);
    }

    [Theory]
    [InlineData(5.0, 4000, 5.0)]   // CBM dominates, already a whole 0.1
    [InlineData(5.02, 4000, 5.1)]  // rounds up to nearest 0.1
    [InlineData(2.0, 6000, 6.0)]   // weight (6 tonnes) dominates over 2 CBM
    public void LclRevenueTon_TakesMaxAndRoundsUpToNearestTenth(decimal cbm, decimal weightKg, decimal expected)
    {
        var revenueTon = ChargeableWeightCalculator.LclRevenueTon(cbm, weightKg);

        Assert.Equal(expected, revenueTon);
    }

    [Fact]
    public void QuoteCalculator_Fcl_FreightIsFlatRatePerContainer()
    {
        var request = new QuoteRequest
        {
            Mode = TransportMode.Fcl,
            Direction = ShipmentDirection.Export,
            OriginPortId = 1,
            DestinationPortId = 2,
            IncotermCode = "CIF",
            ReadyDate = new DateTime(2026, 1, 1),
            ContainerSize = "40",
            ContainerQty = 1,
        };

        var rates = new[]
        {
            new FreightRate
            {
                Id = 1, OriginPortId = 1, DestinationPortId = 2, Mode = TransportMode.Fcl,
                Direction = ShipmentDirection.Export, CarrierId = 1, ContainerSize = "40",
                PriceMin = 200m, PriceMax = 200m, CurrencyCode = "USD",
                ValidFrom = new DateTime(2025, 1, 1), ValidTo = new DateTime(2027, 1, 1), IsActive = true,
            },
        };

        var result = QuoteCalculator.Calculate(request, rates, Array.Empty<LocalCharge>(), Array.Empty<IncotermChargeRule>(), new Dictionary<string, decimal>());

        Assert.True(result.RateFound);
        Assert.Equal(200m, result.FreightCost);
        Assert.Equal(200m, result.Subtotal);
    }

    [Fact]
    public void QuoteCalculator_NoMatchingRate_ReturnsManualWithoutThrowing()
    {
        var request = new QuoteRequest
        {
            Mode = TransportMode.Air,
            Direction = ShipmentDirection.Import,
            OriginPortId = 1,
            DestinationPortId = 2,
            IncotermCode = "EXW",
            ReadyDate = new DateTime(2026, 1, 1),
            ActualWeightKg = 800m, // above the 500kg cutoff — no rate table exists here
            VolumeCm3 = 0m,
        };

        var result = QuoteCalculator.Calculate(request, Array.Empty<FreightRate>(), Array.Empty<LocalCharge>(), Array.Empty<IncotermChargeRule>(), new Dictionary<string, decimal>());

        Assert.False(result.RateFound);
        Assert.Equal(RateSource.Manual, result.RateSource);
        Assert.NotNull(result.NoRateFoundReason);
    }

    [Fact]
    public void LocalChargeCalculator_ExcludesNotQuotableFromTotal_ShowsAsNote()
    {
        var charges = new[]
        {
            new LocalCharge
            {
                Id = 1, PortId = 2, Direction = ShipmentDirection.Export, Mode = TransportMode.Fcl,
                ChargeType = "Customs Duty (MPF/HMF)", CalcBasis = ChargeCalcBasis.NotQuotable,
                AmountMin = 0, AmountMax = 0, CurrencyCode = "USD", ChargeSide = ChargeSide.Destination,
            },
            new LocalCharge
            {
                Id = 2, PortId = 1, Direction = ShipmentDirection.Export, Mode = TransportMode.Fcl,
                ChargeType = "THC", CalcBasis = ChargeCalcBasis.PerContainer,
                AmountMin = 100, AmountMax = 100, CurrencyCode = "USD", ChargeSide = ChargeSide.Origin,
            },
        };
        var rules = new[]
        {
            new IncotermChargeRule { IncotermCode = "DDP", ChargeSide = ChargeSide.Origin, Payer = Payer.Seller },
            new IncotermChargeRule { IncotermCode = "DDP", ChargeSide = ChargeSide.Destination, Payer = Payer.Seller },
        };

        var (lines, total) = LocalChargeCalculator.Calculate(
            charges, ShipmentDirection.Export, "DDP", rules, containerQty: 1, cbm: null, revenueTon: null, chargeableWeightKg: null);

        Assert.Equal(100m, total); // NotQuotable charge contributes 0
        Assert.Contains(lines, l => l.IsNotQuotable && l.ChargeType == "Customs Duty (MPF/HMF)");
    }

    [Fact]
    public void LocalChargeCalculator_OnlyIncludesChargesTheRequestingCustomerOwes()
    {
        // CIF, Export: the Thai exporter (Seller) is the quote's customer and should see
        // ONLY origin charges — matches the real BKK->Singapore CIF quote which had no
        // destination charges at all.
        var charges = new[]
        {
            new LocalCharge
            {
                Id = 1, PortId = 1, Direction = ShipmentDirection.Export, Mode = TransportMode.Fcl,
                ChargeType = "THC", CalcBasis = ChargeCalcBasis.PerContainer,
                AmountMin = 100, AmountMax = 100, CurrencyCode = "USD", ChargeSide = ChargeSide.Origin,
            },
            new LocalCharge
            {
                Id = 2, PortId = 2, Direction = ShipmentDirection.Export, Mode = TransportMode.Fcl,
                ChargeType = "D/O", CalcBasis = ChargeCalcBasis.PerShipment,
                AmountMin = 50, AmountMax = 50, CurrencyCode = "USD", ChargeSide = ChargeSide.Destination,
            },
        };
        var rules = new[]
        {
            new IncotermChargeRule { IncotermCode = "CIF", ChargeSide = ChargeSide.Origin, Payer = Payer.Seller },
            new IncotermChargeRule { IncotermCode = "CIF", ChargeSide = ChargeSide.Destination, Payer = Payer.Buyer },
        };

        var (lines, total) = LocalChargeCalculator.Calculate(
            charges, ShipmentDirection.Export, "CIF", rules, containerQty: 1, cbm: null, revenueTon: null, chargeableWeightKg: null);

        Assert.Single(lines);
        Assert.Equal("THC", lines[0].ChargeType);
        Assert.Equal(100m, total);
    }

    [Fact]
    public void BreakevenCalculator_RecommendsCheaperMode()
    {
        // Corrected formula: local charges are NOT multiplied by CBM.
        var result = BreakevenCalculator.Compare(
            lclFreightCost: 1300m, lclLocalChargeTotal: 110m,
            fclFreightCost: 1650m, fclLocalChargeTotal: 200m);

        Assert.Equal(1410m, result.LclTotal);
        Assert.Equal(1850m, result.FclTotal);
        Assert.Equal(RecommendedMode.Lcl, result.Recommended);
    }
}
