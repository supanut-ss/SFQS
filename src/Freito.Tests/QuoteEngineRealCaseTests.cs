using Freito.Domain.Entities;
using Freito.Domain.Enums;
using Freito.Domain.Quoting;
using Xunit;

namespace Freito.Tests;

/// <summary>
/// T6 — closes the gaps flagged in work-plan.md: multi-currency FX rate resolution,
/// alternative-carrier lists, MinimumCharge floors, PerCBM (vs PerRevenueTon) local charges,
/// and full end-to-end FCL/Air totals built from real rate cards in operation-worksheet.md
/// §4 (not invented numbers — every price here is copied from a real quote or rate card;
/// only the illustrative quantities Operation never provided, like a specific CBM, are chosen
/// by us). QuoteEngineTests.cs covers the original T5 regression set; this file is additive.
/// </summary>
public class QuoteEngineRealCaseTests
{
    // ---- RateResolver: multi-currency + alternatives (previously untested) ----

    [Fact]
    public void RateResolver_PicksCheaperOfTwoCurrencies_UsingBaseCurrencyComparison()
    {
        // USD is the system's base currency (technical-plan.md §7). USD 200 vs THB 15,000
        // (15,000 * 0.028 = 420 USD equivalent) — the USD rate must win even though 15,000
        // looks like a bigger number at a glance.
        var usdRate = new FreightRate
        {
            Id = 1, OriginPortId = 1, DestinationPortId = 2, Mode = TransportMode.Fcl, Direction = ShipmentDirection.Export,
            CarrierId = 1, ContainerSize = "40", PriceMin = 200m, PriceMax = 200m, CurrencyCode = "USD",
            ValidFrom = new DateTime(2020, 1, 1), ValidTo = new DateTime(2030, 1, 1), IsActive = true,
        };
        var thbRate = new FreightRate
        {
            Id = 2, OriginPortId = 1, DestinationPortId = 2, Mode = TransportMode.Fcl, Direction = ShipmentDirection.Export,
            CarrierId = 2, ContainerSize = "40", PriceMin = 15000m, PriceMax = 15000m, CurrencyCode = "THB",
            ValidFrom = new DateTime(2020, 1, 1), ValidTo = new DateTime(2030, 1, 1), IsActive = true,
        };
        var ratesToBase = new Dictionary<string, decimal> { ["THB"] = 0.028m };

        var result = RateResolver.Resolve([usdRate, thbRate], new DateTime(2026, 1, 1), "40", null, ratesToBase);

        Assert.Equal(1, result.Rate!.Id);
        Assert.Single(result.Alternatives);
        Assert.Equal(2, result.Alternatives[0].Id);
    }

    [Fact]
    public void RateResolver_ReturnsAlternatives_SortedByPriceAscending_ExcludingWinner()
    {
        var rates = new[]
        {
            Rate(1, carrierId: 1, price: 300m),
            Rate(2, carrierId: 2, price: 200m),
            Rate(3, carrierId: 3, price: 250m),
        };

        var result = RateResolver.Resolve(rates, new DateTime(2026, 1, 1), "40", null, new Dictionary<string, decimal>());

        Assert.Equal(2, result.Rate!.Id); // 200 is cheapest
        Assert.Equal([3, 1], result.Alternatives.Select(r => r.Id)); // 250 then 300
    }

    [Fact]
    public void RateResolver_CandidateInUnknownCurrency_ThrowsRatherThanSilentlyMisprice()
    {
        var eurRate = Rate(1, carrierId: 1, price: 500m, currency: "EUR");

        Assert.Throws<MissingExchangeRateException>(() =>
            RateResolver.Resolve([eurRate], new DateTime(2026, 1, 1), "40", null, new Dictionary<string, decimal>()));
    }

    private static FreightRate Rate(int id, int carrierId, decimal price, string currency = "USD") => new()
    {
        Id = id, OriginPortId = 1, DestinationPortId = 2, Mode = TransportMode.Fcl, Direction = ShipmentDirection.Export,
        CarrierId = carrierId, ContainerSize = "40", PriceMin = price, PriceMax = price, CurrencyCode = currency,
        ValidFrom = new DateTime(2020, 1, 1), ValidTo = new DateTime(2030, 1, 1), IsActive = true,
    };

    // ---- LocalChargeCalculator: MinimumCharge floor + PerCBM (previously untested) ----

    [Fact]
    public void LocalChargeCalculator_MinimumChargeFloor_AppliesWhenBelowFloor()
    {
        // Real shape from the Barcelona Air quote (operation-worksheet.md §4): PNS-Xray is
        // 0.2/kg with a €65 floor — at a small chargeable weight the floor, not the per-kg
        // rate, determines the charge.
        var charges = new[]
        {
            new LocalCharge
            {
                Id = 1, PortId = 1, Direction = ShipmentDirection.Import, Mode = TransportMode.Air,
                ChargeType = "PNS-Xray", CalcBasis = ChargeCalcBasis.PerKG,
                AmountMin = 0.2m, AmountMax = 0.2m, MinimumCharge = 65m, CurrencyCode = "EUR", ChargeSide = ChargeSide.Origin,
            },
        };
        var rules = new[] { new IncotermChargeRule { IncotermCode = "EXW", ChargeSide = ChargeSide.Origin, Payer = Payer.Buyer } };

        var (lines, total) = LocalChargeCalculator.Calculate(
            charges, ShipmentDirection.Import, "EXW", rules, containerQty: 1, cbm: null, revenueTon: null, chargeableWeightKg: 100m);

        // 0.2 * 100 = 20, which is below the 65 floor — the floor wins.
        Assert.Equal(65m, total);
        Assert.Equal(65m, lines[0].Amount);
    }

    [Fact]
    public void LocalChargeCalculator_MinimumChargeFloor_DoesNotApplyWhenCalculatedAmountIsHigher()
    {
        var charges = new[]
        {
            new LocalCharge
            {
                Id = 1, PortId = 1, Direction = ShipmentDirection.Import, Mode = TransportMode.Air,
                ChargeType = "THC", CalcBasis = ChargeCalcBasis.PerKG,
                AmountMin = 0.19m, AmountMax = 0.19m, MinimumCharge = 5m, CurrencyCode = "EUR", ChargeSide = ChargeSide.Origin,
            },
        };
        var rules = new[] { new IncotermChargeRule { IncotermCode = "EXW", ChargeSide = ChargeSide.Origin, Payer = Payer.Buyer } };

        var (_, total) = LocalChargeCalculator.Calculate(
            charges, ShipmentDirection.Import, "EXW", rules, containerQty: 1, cbm: null, revenueTon: null, chargeableWeightKg: 100m);

        // 0.19 * 100 = 19, comfortably above the 5 floor — the real per-kg amount wins.
        Assert.Equal(19m, total);
    }

    [Fact]
    public void LocalChargeCalculator_PerCBM_ChargesOnRawCbm_NotRevenueTon()
    {
        // Real case: Bangkok -> Jakarta LCL export quote (operation-worksheet.md §4, "ใบที่ 3")
        // charges THC and CFS at THB 100/CBM each — a different basis from the Ningbo->BKK rate
        // cards, which bill the same charge types per revenue ton. This is the one route in the
        // worksheet that confirmed PerCBM must be its own basis, not folded into PerRevenueTon.
        var charges = new[]
        {
            new LocalCharge
            {
                Id = 1, PortId = 1, Direction = ShipmentDirection.Export, Mode = TransportMode.Lcl,
                ChargeType = "THC", CalcBasis = ChargeCalcBasis.PerCBM,
                AmountMin = 100m, AmountMax = 100m, CurrencyCode = "THB", ChargeSide = ChargeSide.Origin,
            },
            new LocalCharge
            {
                Id = 2, PortId = 1, Direction = ShipmentDirection.Export, Mode = TransportMode.Lcl,
                ChargeType = "CFS", CalcBasis = ChargeCalcBasis.PerCBM,
                AmountMin = 100m, AmountMax = 100m, CurrencyCode = "THB", ChargeSide = ChargeSide.Origin,
            },
        };
        var rules = new[] { new IncotermChargeRule { IncotermCode = "FOB", ChargeSide = ChargeSide.Origin, Payer = Payer.Seller } };

        var (_, total) = LocalChargeCalculator.Calculate(
            charges, ShipmentDirection.Export, "FOB", rules, containerQty: 1, cbm: 8.3m, revenueTon: 9m, chargeableWeightKg: null);

        // 100 * 8.3 (raw CBM) twice = 1660 — NOT 100 * 9 (revenue ton) which would be 1800.
        Assert.Equal(1660m, total);
    }

    [Fact]
    public void LocalChargeCalculator_NotQuotable_ExcludedRegardlessOfIncotermOrDirection()
    {
        // "AT COST" storage shows up outside DDP too (operation-worksheet.md §4, "ใบที่ 2" —
        // a plain Import/EXW quote) — NotQuotable exclusion must not be DDP-specific.
        var charges = new[]
        {
            new LocalCharge
            {
                Id = 1, PortId = 1, Direction = ShipmentDirection.Import, Mode = TransportMode.Lcl,
                ChargeType = "Storage", CalcBasis = ChargeCalcBasis.NotQuotable,
                AmountMin = 0, AmountMax = 0, CurrencyCode = "USD", ChargeSide = ChargeSide.Destination,
            },
            new LocalCharge
            {
                Id = 2, PortId = 1, Direction = ShipmentDirection.Import, Mode = TransportMode.Lcl,
                ChargeType = "D/O", CalcBasis = ChargeCalcBasis.PerShipment,
                AmountMin = 1200, AmountMax = 1200, CurrencyCode = "USD", ChargeSide = ChargeSide.Destination,
            },
        };
        var rules = new[] { new IncotermChargeRule { IncotermCode = "EXW", ChargeSide = ChargeSide.Destination, Payer = Payer.Buyer } };

        var (lines, total) = LocalChargeCalculator.Calculate(
            charges, ShipmentDirection.Import, "EXW", rules, containerQty: 1, cbm: null, revenueTon: null, chargeableWeightKg: null);

        Assert.Equal(1200m, total); // Storage contributes 0
        Assert.Contains(lines, l => l.ChargeType == "Storage" && l.IsNotQuotable);
    }

    // ---- Full end-to-end QuoteCalculator totals, real rate card numbers ----

    [Fact]
    public void QuoteCalculator_Fcl_BkkToSingaporeCif_RealRateCardTotal()
    {
        // operation-worksheet.md §4 "เคส 1" — BKK -> Singapore, Export, CIF, 40' container.
        // Freight $200/container (Oceanblu rate card). Local charges (origin only — CIF means
        // the exporter/seller-customer owes nothing at destination): B/L Fee 1,800 + Surrender
        // BL Fee 1,800 + THC 4,300 (40') + B/L Fee 300, all flat per-shipment/per-container as
        // billed on the real invoice. Operation never totalled this themselves (the worksheet
        // says so explicitly) but the arithmetic is plain addition, so this locks in that the
        // engine reproduces it exactly.
        var request = new QuoteRequest
        {
            Mode = TransportMode.Fcl, Direction = ShipmentDirection.Export, OriginPortId = 1, DestinationPortId = 2,
            IncotermCode = "CIF", ReadyDate = new DateTime(2026, 1, 1), ContainerSize = "40", ContainerQty = 1,
        };
        var rates = new[] { Rate(1, carrierId: 1, price: 200m) };
        var localCharges = new[]
        {
            new LocalCharge { Id = 1, PortId = 1, Direction = ShipmentDirection.Export, Mode = TransportMode.Fcl, ChargeType = "B/L Fee", CalcBasis = ChargeCalcBasis.PerShipment, AmountMin = 1800, AmountMax = 1800, CurrencyCode = "USD", ChargeSide = ChargeSide.Origin },
            new LocalCharge { Id = 2, PortId = 1, Direction = ShipmentDirection.Export, Mode = TransportMode.Fcl, ChargeType = "Surrender BL Fee", CalcBasis = ChargeCalcBasis.PerShipment, AmountMin = 1800, AmountMax = 1800, CurrencyCode = "USD", ChargeSide = ChargeSide.Origin },
            new LocalCharge { Id = 3, PortId = 1, Direction = ShipmentDirection.Export, Mode = TransportMode.Fcl, ChargeType = "THC", CalcBasis = ChargeCalcBasis.PerContainer, AmountMin = 4300, AmountMax = 4300, CurrencyCode = "USD", ChargeSide = ChargeSide.Origin },
            new LocalCharge { Id = 4, PortId = 1, Direction = ShipmentDirection.Export, Mode = TransportMode.Fcl, ChargeType = "B/L Fee 2", CalcBasis = ChargeCalcBasis.PerShipment, AmountMin = 300, AmountMax = 300, CurrencyCode = "USD", ChargeSide = ChargeSide.Origin },
        };
        var rules = new[]
        {
            new IncotermChargeRule { IncotermCode = "CIF", ChargeSide = ChargeSide.Origin, Payer = Payer.Seller },
            new IncotermChargeRule { IncotermCode = "CIF", ChargeSide = ChargeSide.Destination, Payer = Payer.Buyer },
        };

        var result = QuoteCalculator.Calculate(request, rates, localCharges, rules, new Dictionary<string, decimal>());

        Assert.True(result.RateFound);
        Assert.Equal(200m, result.FreightCost);
        Assert.Equal(8200m, result.LocalChargeTotal); // 1800+1800+4300+300
        Assert.Equal(8400m, result.Subtotal);
    }

    [Fact]
    public void QuoteCalculator_Air_BarcelonaOriginCharges_RealPerKgRateCardWithFloors()
    {
        // operation-worksheet.md §4 "เคส Air" — Barcelona origin local charges are per-kg with
        // per-line minimums (SAF/WAR no floor, PNS-Xray floor €65, THC floor €5) plus flat fees.
        // The real invoice used 8,916.4kg (above our system's 500kg manual cutoff — see
        // technical-plan.md §3), so this test uses a lighter, in-range weight to exercise the
        // same rate card's PerKG + MinimumCharge interaction end-to-end through QuoteCalculator
        // rather than reproducing the real total, which our engine wouldn't auto-price anyway.
        var request = new QuoteRequest
        {
            Mode = TransportMode.Air, Direction = ShipmentDirection.Import, OriginPortId = 1, DestinationPortId = 2,
            IncotermCode = "EXW", ReadyDate = new DateTime(2026, 1, 1), ActualWeightKg = 100m, VolumeCm3 = 0m,
        };
        var rates = new[]
        {
            new FreightRate
            {
                Id = 1, OriginPortId = 1, DestinationPortId = 2, Mode = TransportMode.Air, Direction = ShipmentDirection.Import,
                CarrierId = 1, WeightBreakMin = 50m, WeightBreakMax = 500m, PriceMin = 1.85m, PriceMax = 1.85m, CurrencyCode = "EUR",
                ValidFrom = new DateTime(2020, 1, 1), ValidTo = new DateTime(2030, 1, 1), IsActive = true,
            },
        };
        var localCharges = new[]
        {
            new LocalCharge { Id = 1, PortId = 1, Direction = ShipmentDirection.Import, Mode = TransportMode.Air, ChargeType = "SAF/WAR", CalcBasis = ChargeCalcBasis.PerKG, AmountMin = 0.18m, AmountMax = 0.18m, CurrencyCode = "EUR", ChargeSide = ChargeSide.Origin },
            new LocalCharge { Id = 2, PortId = 1, Direction = ShipmentDirection.Import, Mode = TransportMode.Air, ChargeType = "PNS-Xray", CalcBasis = ChargeCalcBasis.PerKG, AmountMin = 0.2m, AmountMax = 0.2m, MinimumCharge = 65m, CurrencyCode = "EUR", ChargeSide = ChargeSide.Origin },
            new LocalCharge { Id = 3, PortId = 1, Direction = ShipmentDirection.Import, Mode = TransportMode.Air, ChargeType = "THC", CalcBasis = ChargeCalcBasis.PerKG, AmountMin = 0.19m, AmountMax = 0.19m, MinimumCharge = 5m, CurrencyCode = "EUR", ChargeSide = ChargeSide.Origin },
            new LocalCharge { Id = 4, PortId = 1, Direction = ShipmentDirection.Import, Mode = TransportMode.Air, ChargeType = "Export custom", CalcBasis = ChargeCalcBasis.PerShipment, AmountMin = 98m, AmountMax = 98m, CurrencyCode = "EUR", ChargeSide = ChargeSide.Origin },
            new LocalCharge { Id = 5, PortId = 1, Direction = ShipmentDirection.Import, Mode = TransportMode.Air, ChargeType = "Handling", CalcBasis = ChargeCalcBasis.PerShipment, AmountMin = 70m, AmountMax = 70m, CurrencyCode = "EUR", ChargeSide = ChargeSide.Origin },
            new LocalCharge { Id = 6, PortId = 1, Direction = ShipmentDirection.Import, Mode = TransportMode.Air, ChargeType = "Data fee", CalcBasis = ChargeCalcBasis.PerShipment, AmountMin = 20m, AmountMax = 20m, CurrencyCode = "EUR", ChargeSide = ChargeSide.Origin },
        };
        var rules = new[] { new IncotermChargeRule { IncotermCode = "EXW", ChargeSide = ChargeSide.Origin, Payer = Payer.Buyer } };
        var ratesToBase = new Dictionary<string, decimal> { ["EUR"] = 1.08m }; // only needed for RateResolver's ranking step

        var result = QuoteCalculator.Calculate(request, rates, localCharges, rules, ratesToBase);

        Assert.True(result.RateFound);
        Assert.Equal(100m, result.ChargeableWeightKg); // above the 50kg minimum, so unchanged
        Assert.Equal(185m, result.FreightCost); // 1.85 * 100
        // SAF/WAR 18 + PNS-Xray floor 65 (raw 20) + THC 19 (raw 19, no floor hit) + 98 + 70 + 20
        Assert.Equal(290m, result.LocalChargeTotal);
        Assert.Equal(475m, result.Subtotal);
    }

    // ---- BreakevenCalculator edge case ----

    [Fact]
    public void BreakevenCalculator_Tie_RecommendsLcl()
    {
        // The comparison uses <=, so an exact tie must resolve to LCL, not throw or flip
        // unpredictably — locking that documented default in.
        var result = BreakevenCalculator.Compare(
            lclFreightCost: 1000m, lclLocalChargeTotal: 100m,
            fclFreightCost: 900m, fclLocalChargeTotal: 200m);

        Assert.Equal(1100m, result.LclTotal);
        Assert.Equal(1100m, result.FclTotal);
        Assert.Equal(RecommendedMode.Lcl, result.Recommended);
    }
}
