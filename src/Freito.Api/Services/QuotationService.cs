using System.Globalization;
using Freito.Api.Models;
using Freito.Domain.Entities;
using Freito.Domain.Enums;
using Freito.Domain.Quoting;
using Freito.Domain.Workflow;
using Freito.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Freito.Api.Services;

public enum QuotationActionOutcome
{
    Success,
    NotFound,
    InvalidTransition,
}

public sealed record QuotationActionResult(QuotationActionOutcome Outcome, Quotation? Quotation = null, string? Error = null);

/// <summary>
/// Bridges the pure QuoteCalculator (T5) with real data and the Quotation aggregate (T7):
/// loads candidates, runs the engine, then normalizes the result into ONE display currency
/// (quote_currency) so a quote issued with THB local charges next to a USD freight rate still
/// shows a single coherent total — QuoteCalculator itself doesn't do this conversion, it only
/// sums each side in its own currency. See technical-plan.md §3/§4.
/// </summary>
public sealed class QuotationService(FreitoDbContext db, AuditLogWriter audit)
{
    private const string BaseCurrency = "USD"; // confirmed with Operation, technical-plan.md §7

    public async Task<EntityValidationResult> ValidateAsync(QuoteShipmentRequest request, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        if (request.Mode is null || !Enum.IsDefined(request.Mode.Value)) errors.Add("Mode is required.");
        if (request.Direction is null || !Enum.IsDefined(request.Direction.Value)) errors.Add("Direction is required.");
        if (request.OriginPortId == request.DestinationPortId) errors.Add("Origin and destination must be different ports.");
        if (request.ReadyDate is null) errors.Add("Ready date is required.");

        switch (request.Mode)
        {
            case TransportMode.Fcl:
                if (string.IsNullOrWhiteSpace(request.ContainerSize)) errors.Add("FCL requires a container size.");
                break;
            case TransportMode.Lcl:
                if (request.Cbm is null) errors.Add("LCL requires CBM.");
                if (request.WeightKg is null) errors.Add("LCL requires weight (kg).");
                break;
            case TransportMode.Air:
                if (request.ActualWeightKg is null) errors.Add("Air requires actual weight (kg).");
                if (request.VolumeCm3 is null) errors.Add("Air requires volume (cm3).");
                break;
        }

        if (errors.Count > 0) return new EntityValidationResult(errors);

        var origin = await db.Ports.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.OriginPortId, cancellationToken);
        var destination = await db.Ports.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.DestinationPortId, cancellationToken);
        var incoterm = await db.Incoterms.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Code == request.IncotermCode.ToUpperInvariant(), cancellationToken);
        if (origin is null) errors.Add("Origin port does not exist.");
        if (destination is null) errors.Add("Destination port does not exist.");
        if (incoterm is null) errors.Add("Incoterm does not exist.");

        if (origin is not null && request.Mode is not null)
        {
            var expectedType = request.Mode == TransportMode.Air ? PortType.Air : PortType.Sea;
            if (origin.Type != expectedType) errors.Add($"Origin port must be a {expectedType} port for {request.Mode}.");
        }

        if (destination is not null && request.Mode is not null)
        {
            var expectedType = request.Mode == TransportMode.Air ? PortType.Air : PortType.Sea;
            if (destination.Type != expectedType) errors.Add($"Destination port must be a {expectedType} port for {request.Mode}.");
        }

        return new EntityValidationResult(errors);
    }

    public async Task<QuoteCalculationResponse> ComputeAsync(QuoteShipmentRequest request, CancellationToken cancellationToken)
    {
        var mode = request.Mode!.Value;
        var direction = request.Direction!.Value;
        var incotermCode = request.IncotermCode.ToUpperInvariant();
        var readyDate = DateTime.SpecifyKind(request.ReadyDate!.Value.Date, DateTimeKind.Utc);

        var freightRates = await db.FreightRates.AsNoTracking()
            .Where(r => r.OriginPortId == request.OriginPortId && r.DestinationPortId == request.DestinationPortId &&
                r.Mode == mode && r.Direction == direction)
            .ToListAsync(cancellationToken);

        var localCharges = await db.LocalCharges.AsNoTracking()
            .Where(c => c.Mode == mode && c.Direction == direction &&
                (c.PortId == request.OriginPortId || c.PortId == request.DestinationPortId))
            .ToListAsync(cancellationToken);

        var incotermRules = await db.IncotermChargeRules.AsNoTracking()
            .Where(r => r.IncotermCode == incotermCode)
            .ToListAsync(cancellationToken);

        var ratesToBase = await LatestRatesToBaseAsync(cancellationToken);

        var quoteRequest = new QuoteRequest
        {
            Mode = mode,
            Direction = direction,
            OriginPortId = request.OriginPortId,
            DestinationPortId = request.DestinationPortId,
            IncotermCode = incotermCode,
            ReadyDate = readyDate,
            ContainerSize = string.IsNullOrWhiteSpace(request.ContainerSize) ? null : request.ContainerSize.Trim().ToUpperInvariant(),
            ContainerQty = request.ContainerQty,
            Cbm = request.Cbm,
            WeightKg = request.WeightKg,
            ActualWeightKg = request.ActualWeightKg,
            VolumeCm3 = request.VolumeCm3,
        };

        var result = QuoteCalculator.Calculate(quoteRequest, freightRates, localCharges, incotermRules, ratesToBase);
        var quoteCurrency = result.FreightCurrency ?? BaseCurrency;
        var fxRateUsed = RateToBase(quoteCurrency, ratesToBase);

        var normalizedLines = result.LocalChargeLines
            .Select(line => new LocalChargeLineDto(
                line.ChargeType,
                line.Basis,
                line.IsNotQuotable ? 0 : Convert(line.Amount, line.Currency, quoteCurrency, ratesToBase),
                quoteCurrency,
                line.IsNotQuotable))
            .ToList();
        var normalizedLocalTotal = normalizedLines.Sum(l => l.Amount);
        var subtotal = result.FreightCost + normalizedLocalTotal;

        var alternatives = result.RateFound
            ? (await ResolveAlternativesAsync(request, mode, direction, readyDate, quoteCurrency, ratesToBase, cancellationToken))
            : [];

        return new QuoteCalculationResponse
        {
            RateFound = result.RateFound,
            RateSource = result.RateSource,
            QuoteCurrency = quoteCurrency,
            FxRateUsed = fxRateUsed,
            FreightCost = result.FreightCost,
            LocalChargeTotal = normalizedLocalTotal,
            Subtotal = subtotal,
            ChargeableWeightKg = result.ChargeableWeightKg,
            RevenueTon = result.RevenueTon,
            LocalChargeLines = normalizedLines,
            NotQuotableNotes = result.NotQuotableNotes,
            Alternatives = alternatives,
            NoRateFoundReason = result.NoRateFoundReason,
        };
    }

    /// <summary>Persists a Draft quotation from a computed quote, then immediately advances it
    /// into the mandatory Sale queue — "ระบบร่างให้อัตโนมัติ...แล้วส่งเข้าคิว" (requirements.md §6),
    /// the guest form never leaves a quote sitting in Draft.</summary>
    public async Task<(Quotation Quotation, QuoteCalculationResponse Computation)> SubmitAsync(
        QuoteSubmitRequest request, CancellationToken cancellationToken)
    {
        var computation = await ComputeAsync(request, cancellationToken);
        var now = DateTime.UtcNow;

        var quotation = new Quotation
        {
            QuoteNo = await NextQuoteNoAsync(now, cancellationToken),
            CustomerName = request.CustomerName.Trim(),
            CustomerCompany = string.IsNullOrWhiteSpace(request.CustomerCompany) ? null : request.CustomerCompany.Trim(),
            CustomerEmail = request.CustomerEmail.Trim(),
            CustomerPhone = request.CustomerPhone.Trim(),
            OriginPortId = request.OriginPortId,
            DestinationPortId = request.DestinationPortId,
            Direction = request.Direction!.Value,
            Mode = request.Mode!.Value,
            CargoTypeId = request.CargoTypeId,
            Qty = request.Mode == TransportMode.Fcl ? request.ContainerQty : 1,
            ContainerSize = string.IsNullOrWhiteSpace(request.ContainerSize) ? null : request.ContainerSize.Trim().ToUpperInvariant(),
            Cbm = request.Cbm,
            WeightKg = request.WeightKg ?? request.ActualWeightKg,
            IncotermCode = request.IncotermCode.ToUpperInvariant(),
            ReadyDate = DateTime.SpecifyKind(request.ReadyDate!.Value.Date, DateTimeKind.Utc),
            QuoteCurrency = computation.QuoteCurrency,
            FxRateUsed = computation.FxRateUsed,
            RateSource = computation.RateSource,
            FreightCost = computation.FreightCost,
            LocalChargeTotal = computation.LocalChargeTotal,
            Subtotal = computation.Subtotal,
            DiscountAmount = 0,
            FinalPrice = computation.Subtotal,
            Status = QuotationStatus.Draft,
            Version = 1,
            CreatedByUserId = null,
            ExpiresAt = now.AddDays(30), // confirmed with Operation, technical-plan.md §7
        };

        db.Quotations.Add(quotation);
        await db.SaveChangesAsync(cancellationToken);

        db.QuotationLines.AddRange(BuildLines(quotation.Id, computation));
        db.QuotationStatusHistory.AddRange(
            new QuotationStatusHistory { QuotationId = quotation.Id, FromStatus = QuotationStatus.Draft, ToStatus = QuotationStatus.Draft, ActorUserId = null, At = now, Note = "Guest submission" },
            new QuotationStatusHistory { QuotationId = quotation.Id, FromStatus = QuotationStatus.Draft, ToStatus = QuotationStatus.PendingSaleApproval, ActorUserId = null, At = now, Note = "Queued for Sale approval" });
        quotation.Status = QuotationStatus.PendingSaleApproval;
        await db.SaveChangesAsync(cancellationToken);

        return (quotation, computation with { QuoteId = quotation.Id, QuoteNo = quotation.QuoteNo, ExpiresAt = quotation.ExpiresAt });
    }

    /// <summary>Recomputes today's price for a saved quotation's shipment and diffs it against
    /// the QuotationLine snapshot taken when it was drafted — never mutates the quotation or
    /// its lines (see QuotationLine doc comment); Sale decides what to do with the delta.</summary>
    public async Task<RefreshRateResponse?> RefreshRateAsync(int quotationId, CancellationToken cancellationToken)
    {
        var quotation = await db.Quotations.AsNoTracking().FirstOrDefaultAsync(q => q.Id == quotationId, cancellationToken);
        if (quotation is null) return null;

        var previousLines = await db.QuotationLines.AsNoTracking()
            .Where(l => l.QuotationId == quotationId).ToListAsync(cancellationToken);
        var previousFreight = previousLines.Where(l => l.SourceRateId is not null || l.Description == "Freight").Sum(l => l.Amount);
        var previousLocalByType = previousLines
            .Where(l => l.SourceRateId is null && l.Description != "Freight")
            .ToDictionary(l => l.Description, l => l.Amount);

        var shipment = new QuoteShipmentRequest
        {
            Mode = quotation.Mode,
            Direction = quotation.Direction,
            OriginPortId = quotation.OriginPortId,
            DestinationPortId = quotation.DestinationPortId,
            IncotermCode = quotation.IncotermCode,
            ReadyDate = DateTime.UtcNow.Date, // "refresh rate" re-prices against today, not the original ready date
            ContainerSize = quotation.ContainerSize,
            ContainerQty = quotation.Mode == TransportMode.Fcl ? quotation.Qty : 1,
            Cbm = quotation.Cbm,
            WeightKg = quotation.Mode == TransportMode.Lcl ? quotation.WeightKg : null,
            ActualWeightKg = quotation.Mode == TransportMode.Air ? quotation.WeightKg : null,
            VolumeCm3 = null,
        };

        var current = await ComputeAsync(shipment, cancellationToken);

        var localDeltas = new List<RefreshRateLineDelta>();
        foreach (var line in current.LocalChargeLines.Where(l => !l.IsNotQuotable))
        {
            previousLocalByType.TryGetValue(line.ChargeType, out var previousAmount);
            localDeltas.Add(new RefreshRateLineDelta(line.ChargeType, previousAmount, line.Amount, line.Amount - previousAmount));
        }

        return new RefreshRateResponse(
            PreviousFreightCost: previousFreight,
            CurrentFreightCost: current.FreightCost,
            FreightDelta: current.FreightCost - previousFreight,
            PreviousSubtotal: quotation.Subtotal,
            CurrentSubtotal: current.Subtotal,
            SubtotalDelta: current.Subtotal - quotation.Subtotal,
            Currency: current.QuoteCurrency,
            CurrentRateFound: current.RateFound,
            LocalChargeDeltas: localDeltas);
    }

    /// <summary>The mandatory approval gate (AC3, technical-plan.md §3): only legal from
    /// PendingSaleApproval, enforced via QuotationWorkflow so this can't silently drift from
    /// RejectAsync's check. Caller (QuotesController) has already verified the actor is Sale.</summary>
    public async Task<QuotationActionResult> ApproveAsync(
        int quotationId, int actorId, decimal? finalPrice, string? note, CancellationToken cancellationToken)
    {
        var quotation = await db.Quotations.FirstOrDefaultAsync(q => q.Id == quotationId, cancellationToken);
        if (quotation is null) return new QuotationActionResult(QuotationActionOutcome.NotFound);
        if (!QuotationWorkflow.CanApprove(quotation.Status))
            return new QuotationActionResult(QuotationActionOutcome.InvalidTransition, Error: $"Cannot approve a quotation in status {quotation.Status}.");

        var before = AuditLogWriter.Snapshot(quotation);
        var fromStatus = quotation.Status;
        var now = DateTime.UtcNow;
        quotation.FinalPrice = finalPrice ?? quotation.Subtotal;
        quotation.Status = QuotationStatus.ApprovedAndSent;
        quotation.ApprovedByUserId = actorId;
        quotation.ApprovedAt = now;
        quotation.SentAt = now;

        db.QuotationStatusHistory.Add(new QuotationStatusHistory
        {
            QuotationId = quotation.Id,
            FromStatus = fromStatus,
            ToStatus = quotation.Status,
            ActorUserId = actorId,
            At = now,
            Note = note,
        });

        await audit.SaveAsync(actorId, [PendingAuditChange.Updated("Quotation", quotation.Id, before, quotation)], cancellationToken);
        return new QuotationActionResult(QuotationActionOutcome.Success, quotation);
    }

    public async Task<QuotationActionResult> RejectAsync(int quotationId, int actorId, string note, CancellationToken cancellationToken)
    {
        var quotation = await db.Quotations.FirstOrDefaultAsync(q => q.Id == quotationId, cancellationToken);
        if (quotation is null) return new QuotationActionResult(QuotationActionOutcome.NotFound);
        if (!QuotationWorkflow.CanReject(quotation.Status))
            return new QuotationActionResult(QuotationActionOutcome.InvalidTransition, Error: $"Cannot reject a quotation in status {quotation.Status}.");

        var before = AuditLogWriter.Snapshot(quotation);
        var fromStatus = quotation.Status;
        var now = DateTime.UtcNow;
        quotation.Status = QuotationStatus.Rejected;

        db.QuotationStatusHistory.Add(new QuotationStatusHistory
        {
            QuotationId = quotation.Id,
            FromStatus = fromStatus,
            ToStatus = quotation.Status,
            ActorUserId = actorId,
            At = now,
            Note = note,
        });

        await audit.SaveAsync(actorId, [PendingAuditChange.Updated("Quotation", quotation.Id, before, quotation)], cancellationToken);
        return new QuotationActionResult(QuotationActionOutcome.Success, quotation);
    }

    private static IEnumerable<QuotationLine> BuildLines(int quotationId, QuoteCalculationResponse computation)
    {
        if (computation.RateFound)
        {
            yield return new QuotationLine
            {
                QuotationId = quotationId,
                Description = "Freight",
                Basis = computation.RateSource.ToString(),
                UnitPrice = computation.FreightCost,
                Qty = 1,
                Amount = computation.FreightCost,
                Currency = computation.QuoteCurrency,
            };
        }

        foreach (var line in computation.LocalChargeLines.Where(l => !l.IsNotQuotable))
        {
            yield return new QuotationLine
            {
                QuotationId = quotationId,
                Description = line.ChargeType,
                Basis = line.Basis,
                UnitPrice = line.Amount,
                Qty = 1,
                Amount = line.Amount,
                Currency = line.Currency,
            };
        }
    }

    private async Task<IReadOnlyList<FreightAlternativeDto>> ResolveAlternativesAsync(
        QuoteShipmentRequest request,
        TransportMode mode,
        ShipmentDirection direction,
        DateTime readyDate,
        string quoteCurrency,
        IReadOnlyDictionary<string, decimal> ratesToBase,
        CancellationToken cancellationToken)
    {
        var candidates = await db.FreightRates.AsNoTracking()
            .Where(r => r.OriginPortId == request.OriginPortId && r.DestinationPortId == request.DestinationPortId &&
                r.Mode == mode && r.Direction == direction && r.IsActive &&
                readyDate >= r.ValidFrom && readyDate <= r.ValidTo)
            .ToListAsync(cancellationToken);

        var weightForLookup = mode == TransportMode.Air
            ? Math.Max(
                ChargeableWeightCalculator.AirRateLookupWeightKg(request.ActualWeightKg ?? 0, request.VolumeCm3 ?? 0),
                ChargeableWeightCalculator.AirMinimumChargeableWeightKg)
            : (decimal?)null;

        var resolution = RateResolver.Resolve(
            candidates, readyDate,
            mode == TransportMode.Fcl ? request.ContainerSize?.Trim().ToUpperInvariant() : null,
            weightForLookup,
            ratesToBase);

        return resolution.Alternatives
            .Select(r => new FreightAlternativeDto(r.CarrierId, Convert(r.PriceMax, r.CurrencyCode, quoteCurrency, ratesToBase), quoteCurrency))
            .ToList();
    }

    private async Task<string> NextQuoteNoAsync(DateTime now, CancellationToken cancellationToken)
    {
        var prefix = $"Q{now.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}";
        var countToday = await db.Quotations.CountAsync(q => q.QuoteNo.StartsWith(prefix), cancellationToken);
        return $"{prefix}-{countToday + 1:0000}";
    }

    private async Task<Dictionary<string, decimal>> LatestRatesToBaseAsync(CancellationToken cancellationToken)
    {
        var rates = await db.ExchangeRates.AsNoTracking()
            .Where(r => r.EffectiveDate <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);
        return rates
            .GroupBy(r => r.CurrencyCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.EffectiveDate).First().RateToBase, StringComparer.OrdinalIgnoreCase);
    }

    private static decimal RateToBase(string currencyCode, IReadOnlyDictionary<string, decimal> ratesToBase)
    {
        if (currencyCode.Equals(BaseCurrency, StringComparison.OrdinalIgnoreCase)) return 1m;
        return ratesToBase.TryGetValue(currencyCode, out var rate)
            ? rate
            : throw new InvalidOperationException($"No exchange rate available for currency '{currencyCode}'.");
    }

    private static decimal Convert(decimal amount, string fromCurrency, string toCurrency, IReadOnlyDictionary<string, decimal> ratesToBase)
    {
        if (fromCurrency.Equals(toCurrency, StringComparison.OrdinalIgnoreCase)) return amount;
        var baseAmount = amount * RateToBase(fromCurrency, ratesToBase);
        return baseAmount / RateToBase(toCurrency, ratesToBase);
    }
}
