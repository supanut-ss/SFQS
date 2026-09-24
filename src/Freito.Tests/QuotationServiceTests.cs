using Freito.Api.Models;
using Freito.Api.Services;
using Freito.Domain.Entities;
using Freito.Domain.Enums;
using Freito.Domain.Quoting;
using Freito.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Freito.Tests;

/// <summary>
/// T7 — exercises QuotationService (the Quotation API's engine-to-DB bridge) against an
/// in-memory provider seeded like a real deployment: ports/carriers/rates as Operation would
/// enter them, currencies/incoterms as migration-seeded. Complements QuoteEngineTests (T5,
/// pure calculator) by covering persistence, currency normalization, and the mandatory
/// Draft -> PendingSaleApproval transition (requirements.md §6).
/// </summary>
public class QuotationServiceTests
{
    private static FreitoDbContext CreateSeededContext()
    {
        var options = new DbContextOptionsBuilder<FreitoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new FreitoDbContext(options);

        db.Ports.AddRange(
            new Port { Id = 1, Code = "THBKK", Name = "Bangkok Port", City = "Bangkok", Country = "Thailand", Type = PortType.Sea },
            new Port { Id = 2, Code = "SGSIN", Name = "Singapore Port", City = "Singapore", Country = "Singapore", Type = PortType.Sea });
        db.Carriers.Add(new Carrier { Id = 1, Code = "MAERSK", Name = "Maersk", Type = CarrierType.ShippingLine });
        db.CargoTypes.Add(new CargoType { Id = 1, Name = "General Cargo" });
        db.Currencies.AddRange(
            new Currency { Code = "USD", Name = "US Dollar", DecimalDigits = 2 },
            new Currency { Code = "THB", Name = "Thai Baht", DecimalDigits = 2 });
        db.ExchangeRates.AddRange(
            new ExchangeRate { CurrencyCode = "USD", RateToBase = 1m, EffectiveDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ExchangeRate { CurrencyCode = "THB", RateToBase = 0.028m, EffectiveDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc) });
        db.Incoterms.Add(new Incoterm { Code = "CIF", Name = "Cost, Insurance and Freight", RiskTransferPoint = "On board vessel", SellerPaysFreight = true });
        db.IncotermChargeRules.AddRange(
            new IncotermChargeRule { IncotermCode = "CIF", ChargeSide = ChargeSide.Origin, Payer = Payer.Seller },
            new IncotermChargeRule { IncotermCode = "CIF", ChargeSide = ChargeSide.Destination, Payer = Payer.Buyer });
        db.FreightRates.Add(new FreightRate
        {
            Id = 1, OriginPortId = 1, DestinationPortId = 2, Mode = TransportMode.Fcl,
            Direction = ShipmentDirection.Export, CarrierId = 1, ContainerSize = "40",
            PriceMin = 200m, PriceMax = 200m, CurrencyCode = "USD",
            ValidFrom = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ValidTo = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc), IsActive = true,
        });
        db.LocalCharges.Add(new LocalCharge
        {
            Id = 1, PortId = 1, Direction = ShipmentDirection.Export, Mode = TransportMode.Fcl,
            ChargeType = "THC", CalcBasis = ChargeCalcBasis.PerContainer,
            AmountMin = 1000m, AmountMax = 1000m, CurrencyCode = "THB", ChargeSide = ChargeSide.Origin,
        });
        db.SaveChanges();
        return db;
    }

    private static QuoteShipmentRequest FclRequest() => new()
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

    [Fact]
    public async Task ComputeAsync_UsesFreightRateCurrencyAsQuoteCurrency_AndConvertsLocalCharges()
    {
        var db = CreateSeededContext();
        var service = new QuotationService(db, new AuditLogWriter(db));

        var result = await service.ComputeAsync(FclRequest(), CancellationToken.None);

        Assert.True(result.RateFound);
        Assert.Equal("USD", result.QuoteCurrency);
        Assert.Equal(200m, result.FreightCost);
        // 1000 THB * 0.028 USD/THB = 28 USD — local charge normalized into the quote currency.
        Assert.Equal(28m, result.LocalChargeTotal);
        Assert.Equal(228m, result.Subtotal);
        Assert.Single(result.LocalChargeLines);
        Assert.Equal("USD", result.LocalChargeLines[0].Currency);
    }

    [Fact]
    public async Task ComputeAsync_NoRateFound_FallsBackToUsdAndStillPricesLocalCharges()
    {
        var db = CreateSeededContext();
        var service = new QuotationService(db, new AuditLogWriter(db));
        var request = FclRequest() with { ReadyDate = new DateTime(2050, 1, 1) }; // outside the seeded rate's validity window

        var result = await service.ComputeAsync(request, CancellationToken.None);

        Assert.False(result.RateFound);
        Assert.Equal("USD", result.QuoteCurrency);
        Assert.Equal(0m, result.FreightCost);
        Assert.Equal(28m, result.LocalChargeTotal);
        Assert.NotNull(result.NoRateFoundReason);
    }

    [Fact]
    public async Task ComputeAsync_MissingLocalChargeExchangeRate_ThrowsTypedException()
    {
        var db = CreateSeededContext();
        var thbRate = await db.ExchangeRates.SingleAsync(x => x.CurrencyCode == "THB");
        db.ExchangeRates.Remove(thbRate);
        await db.SaveChangesAsync();
        var service = new QuotationService(db, new AuditLogWriter(db));

        var exception = await Assert.ThrowsAsync<MissingExchangeRateException>(
            () => service.ComputeAsync(FclRequest(), CancellationToken.None));

        Assert.Equal("THB", exception.CurrencyCode);
    }

    [Fact]
    public async Task SubmitAsync_PersistsDraftThenAdvancesToPendingSaleApproval()
    {
        var db = CreateSeededContext();
        var service = new QuotationService(db, new AuditLogWriter(db));
        var request = new QuoteSubmitRequest
        {
            Mode = TransportMode.Fcl,
            Direction = ShipmentDirection.Export,
            OriginPortId = 1,
            DestinationPortId = 2,
            IncotermCode = "CIF",
            ReadyDate = new DateTime(2026, 1, 1),
            ContainerSize = "40",
            ContainerQty = 1,
            CustomerName = "Somchai Exports",
            CustomerEmail = "somchai@example.com",
            CustomerPhone = "0812345678",
            CargoTypeId = 1,
        };

        var (quotation, computation) = await service.SubmitAsync(request, CancellationToken.None);

        Assert.Equal(QuotationStatus.PendingSaleApproval, quotation.Status);
        Assert.Null(quotation.CreatedByUserId);
        Assert.StartsWith("Q", quotation.QuoteNo);
        Assert.Equal(228m, quotation.Subtotal);
        Assert.Equal(quotation.Subtotal, quotation.FinalPrice);
        Assert.Equal(quotation.Id, computation.QuoteId);

        var history = await db.QuotationStatusHistory.Where(h => h.QuotationId == quotation.Id).OrderBy(h => h.Id).ToListAsync();
        Assert.Equal(2, history.Count);
        Assert.Equal(QuotationStatus.PendingSaleApproval, history[1].ToStatus);

        var lines = await db.QuotationLines.Where(l => l.QuotationId == quotation.Id).ToListAsync();
        Assert.Equal(2, lines.Count); // Freight + THC
        Assert.Equal(228m, lines.Sum(l => l.Amount));
    }

    [Fact]
    public async Task SubmitAsync_GeneratesUniqueSequentialQuoteNumbersPerDay()
    {
        var db = CreateSeededContext();
        var service = new QuotationService(db, new AuditLogWriter(db));
        var request = new QuoteSubmitRequest
        {
            Mode = TransportMode.Fcl,
            Direction = ShipmentDirection.Export,
            OriginPortId = 1,
            DestinationPortId = 2,
            IncotermCode = "CIF",
            ReadyDate = new DateTime(2026, 1, 1),
            ContainerSize = "40",
            ContainerQty = 1,
            CustomerName = "Customer One",
            CustomerEmail = "one@example.com",
            CustomerPhone = "0812345678",
            CargoTypeId = 1,
        };

        var (first, _) = await service.SubmitAsync(request, CancellationToken.None);
        var (second, _) = await service.SubmitAsync(request with { CustomerName = "Customer Two" }, CancellationToken.None);

        Assert.NotEqual(first.QuoteNo, second.QuoteNo);
    }

    [Fact]
    public async Task RefreshRateAsync_ReturnsZeroDelta_WhenNothingChangedSinceSubmission()
    {
        var db = CreateSeededContext();
        var service = new QuotationService(db, new AuditLogWriter(db));
        var request = new QuoteSubmitRequest
        {
            Mode = TransportMode.Fcl,
            Direction = ShipmentDirection.Export,
            OriginPortId = 1,
            DestinationPortId = 2,
            IncotermCode = "CIF",
            ReadyDate = new DateTime(2026, 1, 1),
            ContainerSize = "40",
            ContainerQty = 1,
            CustomerName = "Somchai Exports",
            CustomerEmail = "somchai@example.com",
            CustomerPhone = "0812345678",
            CargoTypeId = 1,
        };
        var (quotation, _) = await service.SubmitAsync(request, CancellationToken.None);

        var delta = await service.RefreshRateAsync(quotation.Id, CancellationToken.None);

        Assert.NotNull(delta);
        Assert.True(delta!.CurrentRateFound);
        Assert.Equal(0m, delta.FreightDelta);
        Assert.Equal(0m, delta.SubtotalDelta);
        Assert.All(delta.LocalChargeDeltas, d => Assert.Equal(0m, d.Delta));
    }

    [Fact]
    public async Task RefreshRateAsync_ReturnsNull_ForUnknownQuotation()
    {
        var db = CreateSeededContext();
        var service = new QuotationService(db, new AuditLogWriter(db));

        var delta = await service.RefreshRateAsync(999, CancellationToken.None);

        Assert.Null(delta);
    }

    private static async Task<Quotation> SubmitQuotationAsync(FreitoDbContext db)
    {
        var service = new QuotationService(db, new AuditLogWriter(db));
        var request = new QuoteSubmitRequest
        {
            Mode = TransportMode.Fcl,
            Direction = ShipmentDirection.Export,
            OriginPortId = 1,
            DestinationPortId = 2,
            IncotermCode = "CIF",
            ReadyDate = new DateTime(2026, 1, 1),
            ContainerSize = "40",
            ContainerQty = 1,
            CustomerName = "Somchai Exports",
            CustomerEmail = "somchai@example.com",
            CustomerPhone = "0812345678",
            CargoTypeId = 1,
        };
        var (quotation, _) = await service.SubmitAsync(request, CancellationToken.None);
        return quotation;
    }

    [Fact]
    public async Task ApproveAsync_FromPendingSaleApproval_Succeeds()
    {
        var db = CreateSeededContext();
        var quotation = await SubmitQuotationAsync(db);
        var service = new QuotationService(db, new AuditLogWriter(db));

        var result = await service.ApproveAsync(quotation.Id, actorId: 42, finalPrice: 210m, note: "Discount applied", CancellationToken.None);

        Assert.Equal(QuotationActionOutcome.Success, result.Outcome);
        Assert.Equal(QuotationStatus.ApprovedAndSent, result.Quotation!.Status);
        Assert.Equal(210m, result.Quotation.FinalPrice);
        Assert.Equal(42, result.Quotation.ApprovedByUserId);
        Assert.NotNull(result.Quotation.ApprovedAt);
        Assert.NotNull(result.Quotation.SentAt);

        var history = await db.QuotationStatusHistory.Where(h => h.QuotationId == quotation.Id).OrderBy(h => h.Id).ToListAsync();
        Assert.Equal(QuotationStatus.ApprovedAndSent, history.Last().ToStatus);
        Assert.Equal(42, history.Last().ActorUserId);

        var auditEntry = await db.AuditLogs.SingleAsync(a => a.Entity == "Quotation" && a.EntityId == quotation.Id);
        Assert.Equal(42, auditEntry.ChangedByUserId);
    }

    [Fact]
    public async Task ApproveAsync_AlreadyApproved_IsRejectedByTheGate()
    {
        var db = CreateSeededContext();
        var quotation = await SubmitQuotationAsync(db);
        var service = new QuotationService(db, new AuditLogWriter(db));
        await service.ApproveAsync(quotation.Id, actorId: 1, finalPrice: null, note: null, CancellationToken.None);

        var result = await service.ApproveAsync(quotation.Id, actorId: 1, finalPrice: null, note: null, CancellationToken.None);

        Assert.Equal(QuotationActionOutcome.InvalidTransition, result.Outcome);
    }

    [Fact]
    public async Task ApproveAsync_UnknownQuotation_ReturnsNotFound()
    {
        var db = CreateSeededContext();
        var service = new QuotationService(db, new AuditLogWriter(db));

        var result = await service.ApproveAsync(999, actorId: 1, finalPrice: null, note: null, CancellationToken.None);

        Assert.Equal(QuotationActionOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task RejectAsync_FromPendingSaleApproval_Succeeds()
    {
        var db = CreateSeededContext();
        var quotation = await SubmitQuotationAsync(db);
        var service = new QuotationService(db, new AuditLogWriter(db));

        var result = await service.RejectAsync(quotation.Id, actorId: 42, note: "Price too high", CancellationToken.None);

        Assert.Equal(QuotationActionOutcome.Success, result.Outcome);
        Assert.Equal(QuotationStatus.Rejected, result.Quotation!.Status);
        Assert.Null(result.Quotation.ApprovedByUserId);
    }

    [Fact]
    public async Task RejectAsync_AfterAlreadyRejected_IsRejectedByTheGate()
    {
        var db = CreateSeededContext();
        var quotation = await SubmitQuotationAsync(db);
        var service = new QuotationService(db, new AuditLogWriter(db));
        await service.RejectAsync(quotation.Id, actorId: 1, note: "First rejection", CancellationToken.None);

        var result = await service.RejectAsync(quotation.Id, actorId: 1, note: "Second attempt", CancellationToken.None);

        Assert.Equal(QuotationActionOutcome.InvalidTransition, result.Outcome);
    }

    [Fact]
    public async Task UpdateLinesAsync_ModifiesLinesAndRecalculatesTotals()
    {
        var db = CreateSeededContext();
        var quotation = await SubmitQuotationAsync(db);
        var service = new QuotationService(db, new AuditLogWriter(db));

        var customizedLines = new List<QuoteLineItemDto>
        {
            new() { Description = "Freight", Basis = "Flat", UnitPrice = 250m, Qty = 1, Amount = 250m, Currency = "USD" },
            new() { Description = "THC (Destination)", Basis = "PerContainer", UnitPrice = 50m, Qty = 1, Amount = 50m, Currency = "USD" },
            new() { Description = "Trucking / Pick up", Basis = "Per truck", UnitPrice = 120m, Qty = 1, Amount = 120m, Currency = "USD" },
        };

        var result = await service.UpdateLinesAsync(
            quotation.Id, actorId: 42, customizedLines, finalPrice: 400m, note: "Custom discounted package", CancellationToken.None);

        Assert.Equal(QuotationActionOutcome.Success, result.Outcome);
        Assert.Equal(420m, result.Quotation!.Subtotal); // 250 + 50 + 120
        Assert.Equal(400m, result.Quotation.FinalPrice);
        Assert.Equal(20m, result.Quotation.DiscountAmount); // 420 - 400
        Assert.Equal(250m, result.Quotation.FreightCost);
        Assert.Equal(170m, result.Quotation.LocalChargeTotal); // 50 + 120

        var lines = await db.QuotationLines.Where(l => l.QuotationId == quotation.Id).ToListAsync();
        Assert.Equal(3, lines.Count);
        Assert.Contains(lines, l => l.Description == "Trucking / Pick up" && l.Amount == 120m);
    }

    [Fact]
    public async Task ApproveAsync_WithCustomLines_UpdatesLinesAndApproves()
    {
        var db = CreateSeededContext();
        var quotation = await SubmitQuotationAsync(db);
        var service = new QuotationService(db, new AuditLogWriter(db));

        var customizedLines = new List<QuoteLineItemDto>
        {
            new() { Description = "Freight", Basis = "Flat", Amount = 180m, Currency = "USD" },
            new() { Description = "Custom clearance", Basis = "Shipment", Amount = 70m, Currency = "USD" },
        };

        var result = await service.ApproveAsync(
            quotation.Id, actorId: 42, finalPrice: 250m, note: "Approved with custom fee", CancellationToken.None, customizedLines);

        Assert.Equal(QuotationActionOutcome.Success, result.Outcome);
        Assert.Equal(QuotationStatus.ApprovedAndSent, result.Quotation!.Status);
        Assert.Equal(250m, result.Quotation.Subtotal); // 180 + 70
        Assert.Equal(250m, result.Quotation.FinalPrice);

        var lines = await db.QuotationLines.Where(l => l.QuotationId == quotation.Id).ToListAsync();
        Assert.Equal(2, lines.Count);
    }
}
