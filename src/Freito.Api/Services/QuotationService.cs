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
                if (request.Cbm is decimal cbm && decimal.Round(cbm, 3) != cbm)
                    errors.Add("CBM supports at most three decimal places.");
                if (request.WeightKg is decimal lclWeight && decimal.Round(lclWeight, 3) != lclWeight)
                    errors.Add("Weight supports at most three decimal places.");
                break;
            case TransportMode.Air:
                if (request.ActualWeightKg is null) errors.Add("Air requires actual weight (kg).");
                if (request.VolumeCm3 is null) errors.Add("Air requires volume (cm3).");
                if (request.ActualWeightKg is decimal airWeight && decimal.Round(airWeight, 3) != airWeight)
                    errors.Add("Weight supports at most three decimal places.");
                if (request.VolumeCm3 is decimal volume && decimal.Round(volume, 3) != volume)
                    errors.Add("Volume supports at most three decimal places.");
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

        db.QuotationLines.AddRange(BuildLines(quotation.Id, request, computation));
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

    public async Task<QuotationActionResult> UpdateLinesAsync(
        int quotationId,
        int actorId,
        IReadOnlyList<QuoteLineItemDto> lines,
        decimal? finalPrice,
        string? note,
        CancellationToken cancellationToken,
        string? transitTime = null,
        string? frequency = null,
        string? closingSchedule = null,
        string? carrierInfo = null,
        string? paymentTerms = null,
        string? insuranceStatus = null,
        string? termsAndConditions = null,
        string? dimensionsJson = null,
        string? customerName = null,
        string? customerCompany = null,
        string? customerEmail = null,
        string? customerPhone = null,
        int? originPortId = null,
        int? destinationPortId = null,
        ShipmentDirection? direction = null,
        TransportMode? mode = null,
        int? cargoTypeId = null,
        int? qty = null,
        string? containerSize = null,
        decimal? cbm = null,
        decimal? weightKg = null,
        string? incotermCode = null,
        DateTime? readyDate = null)
    {
        var quotation = await db.Quotations.FirstOrDefaultAsync(q => q.Id == quotationId, cancellationToken);
        if (quotation is null) return new QuotationActionResult(QuotationActionOutcome.NotFound);
        if (quotation.Status is not (QuotationStatus.PendingSaleApproval or QuotationStatus.Draft or QuotationStatus.Approved))
            return new QuotationActionResult(QuotationActionOutcome.InvalidTransition, Error: $"Cannot modify lines for a quotation in status {quotation.Status}.");

        var existingLines = await db.QuotationLines.Where(l => l.QuotationId == quotationId).ToListAsync(cancellationToken);
        var before = AuditLogWriter.Snapshot(quotation);

        db.QuotationLines.RemoveRange(existingLines);

        var newLines = lines.Select(l => new QuotationLine
        {
            QuotationId = quotation.Id,
            Description = string.IsNullOrWhiteSpace(l.Description) ? "Charge" : l.Description.Trim(),
            Basis = string.IsNullOrWhiteSpace(l.Basis) ? "Per shipment" : l.Basis.Trim(),
            UnitPrice = l.UnitPrice != 0 ? l.UnitPrice : l.Amount,
            Qty = l.Qty > 0 ? l.Qty : 1,
            Amount = l.Amount,
            Currency = string.IsNullOrWhiteSpace(l.Currency) ? quotation.QuoteCurrency : l.Currency.Trim().ToUpperInvariant(),
        }).ToList();

        db.QuotationLines.AddRange(newLines);

        var freightLines = newLines.Where(l => l.Description.Equals("Freight", StringComparison.OrdinalIgnoreCase)).ToList();
        var localLines = newLines.Where(l => !l.Description.Equals("Freight", StringComparison.OrdinalIgnoreCase)).ToList();

        quotation.FreightCost = freightLines.Sum(l => l.Amount);
        quotation.LocalChargeTotal = localLines.Sum(l => l.Amount);
        quotation.Subtotal = newLines.Sum(l => l.Amount);

        if (finalPrice.HasValue && finalPrice.Value >= 0)
        {
            quotation.FinalPrice = finalPrice.Value;
            quotation.DiscountAmount = Math.Max(0, quotation.Subtotal - quotation.FinalPrice);
        }
        else
        {
            quotation.FinalPrice = quotation.Subtotal;
            quotation.DiscountAmount = 0;
        }

        // Customer edits
        if (!string.IsNullOrWhiteSpace(customerName)) quotation.CustomerName = customerName.Trim();
        if (customerCompany is not null) quotation.CustomerCompany = string.IsNullOrWhiteSpace(customerCompany) ? null : customerCompany.Trim();
        if (!string.IsNullOrWhiteSpace(customerEmail)) quotation.CustomerEmail = customerEmail.Trim();
        if (!string.IsNullOrWhiteSpace(customerPhone)) quotation.CustomerPhone = customerPhone.Trim();

        // Shipment edits
        if (originPortId.HasValue && originPortId.Value > 0) quotation.OriginPortId = originPortId.Value;
        if (destinationPortId.HasValue && destinationPortId.Value > 0) quotation.DestinationPortId = destinationPortId.Value;
        if (direction.HasValue) quotation.Direction = direction.Value;
        if (mode.HasValue) quotation.Mode = mode.Value;
        if (cargoTypeId.HasValue && cargoTypeId.Value > 0) quotation.CargoTypeId = cargoTypeId.Value;
        if (qty.HasValue && qty.Value > 0) quotation.Qty = qty.Value;
        if (containerSize is not null) quotation.ContainerSize = string.IsNullOrWhiteSpace(containerSize) ? null : containerSize.Trim().ToUpperInvariant();
        if (cbm.HasValue) quotation.Cbm = cbm.Value;
        if (weightKg.HasValue) quotation.WeightKg = weightKg.Value;
        if (!string.IsNullOrWhiteSpace(incotermCode)) quotation.IncotermCode = incotermCode.Trim().ToUpperInvariant();
        if (readyDate.HasValue) quotation.ReadyDate = DateTime.SpecifyKind(readyDate.Value.Date, DateTimeKind.Utc);

        // Modular optional section fields
        if (transitTime is not null) quotation.TransitTime = transitTime.Trim();
        if (frequency is not null) quotation.Frequency = frequency.Trim();
        if (closingSchedule is not null) quotation.ClosingSchedule = closingSchedule.Trim();
        if (carrierInfo is not null) quotation.CarrierInfo = carrierInfo.Trim();
        if (paymentTerms is not null) quotation.PaymentTerms = paymentTerms.Trim();
        if (insuranceStatus is not null) quotation.InsuranceStatus = insuranceStatus.Trim();
        if (termsAndConditions is not null) quotation.TermsAndConditions = termsAndConditions.Trim();
        if (dimensionsJson is not null) quotation.DimensionsJson = dimensionsJson.Trim();

        if (!string.IsNullOrWhiteSpace(note))
        {
            db.QuotationStatusHistory.Add(new QuotationStatusHistory
            {
                QuotationId = quotation.Id,
                FromStatus = quotation.Status,
                ToStatus = quotation.Status,
                ActorUserId = actorId,
                At = DateTime.UtcNow,
                Note = $"Lines updated: {note}",
            });
        }

        await audit.SaveAsync(actorId, [PendingAuditChange.Updated("Quotation", quotation.Id, before, quotation)], cancellationToken);
        return new QuotationActionResult(QuotationActionOutcome.Success, quotation);
    }

    /// <summary>The mandatory approval gate (AC3, technical-plan.md §3): only legal from
    /// PendingSaleApproval, enforced via QuotationWorkflow so this can't silently drift from
    /// RejectAsync's check. Caller (QuotesController) has already verified the actor is Sale or Admin.
    /// Note: Approving marks the quotation as Approved and records ApprovedAt, but does NOT send it
    /// to the customer yet; Sale/Admin dispatches via SendAsync manually.</summary>
    public async Task<QuotationActionResult> ApproveAsync(
        int quotationId,
        int actorId,
        decimal? finalPrice,
        string? note,
        CancellationToken cancellationToken,
        IReadOnlyList<QuoteLineItemDto>? lines = null,
        string? transitTime = null,
        string? frequency = null,
        string? closingSchedule = null,
        string? carrierInfo = null,
        string? paymentTerms = null,
        string? insuranceStatus = null,
        string? termsAndConditions = null,
        string? dimensionsJson = null,
        string? customerName = null,
        string? customerCompany = null,
        string? customerEmail = null,
        string? customerPhone = null,
        int? originPortId = null,
        int? destinationPortId = null,
        ShipmentDirection? direction = null,
        TransportMode? mode = null,
        int? cargoTypeId = null,
        int? qty = null,
        string? containerSize = null,
        decimal? cbm = null,
        decimal? weightKg = null,
        string? incotermCode = null,
        DateTime? readyDate = null)
    {
        var hasEdits = lines is { Count: > 0 } || transitTime is not null || frequency is not null || closingSchedule is not null ||
            carrierInfo is not null || paymentTerms is not null || insuranceStatus is not null || termsAndConditions is not null || dimensionsJson is not null ||
            customerName is not null || customerCompany is not null || customerEmail is not null || customerPhone is not null ||
            originPortId is not null || destinationPortId is not null || direction is not null || mode is not null ||
            cargoTypeId is not null || qty is not null || containerSize is not null || cbm is not null || weightKg is not null ||
            incotermCode is not null || readyDate is not null;

        if (hasEdits)
        {
            var updateResult = await UpdateLinesAsync(
                quotationId, actorId, lines ?? [], finalPrice, null, cancellationToken,
                transitTime, frequency, closingSchedule, carrierInfo, paymentTerms, insuranceStatus, termsAndConditions, dimensionsJson,
                customerName, customerCompany, customerEmail, customerPhone, originPortId, destinationPortId, direction, mode,
                cargoTypeId, qty, containerSize, cbm, weightKg, incotermCode, readyDate);
            if (updateResult.Outcome != QuotationActionOutcome.Success)
                return updateResult;
        }

        var quotation = await db.Quotations.FirstOrDefaultAsync(q => q.Id == quotationId, cancellationToken);
        if (quotation is null) return new QuotationActionResult(QuotationActionOutcome.NotFound);
        if (!QuotationWorkflow.CanApprove(quotation.Status))
            return new QuotationActionResult(QuotationActionOutcome.InvalidTransition, Error: $"Cannot approve a quotation in status {quotation.Status}.");

        var before = AuditLogWriter.Snapshot(quotation);
        var fromStatus = quotation.Status;
        var now = DateTime.UtcNow;
        quotation.FinalPrice = finalPrice ?? quotation.FinalPrice;
        quotation.DiscountAmount = Math.Max(0, quotation.Subtotal - quotation.FinalPrice);
        quotation.Status = QuotationStatus.Approved;
        quotation.ApprovedByUserId = actorId;
        quotation.ApprovedAt = now;

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

    /// <summary>Decoupled manual dispatch: Sale/Admin explicitly dispatches quotation to customer.
    /// Advances Approved -> ApprovedAndSent and records SentAt.</summary>
    public async Task<QuotationActionResult> SendAsync(
        int quotationId,
        int actorId,
        string? note,
        CancellationToken cancellationToken)
    {
        var quotation = await db.Quotations.FirstOrDefaultAsync(q => q.Id == quotationId, cancellationToken);
        if (quotation is null) return new QuotationActionResult(QuotationActionOutcome.NotFound);
        if (!QuotationWorkflow.CanSend(quotation.Status))
            return new QuotationActionResult(QuotationActionOutcome.InvalidTransition, Error: $"Cannot send a quotation in status {quotation.Status}.");

        var before = AuditLogWriter.Snapshot(quotation);
        var fromStatus = quotation.Status;
        var now = DateTime.UtcNow;
        quotation.Status = QuotationStatus.ApprovedAndSent;
        quotation.SentAt = now;

        db.QuotationStatusHistory.Add(new QuotationStatusHistory
        {
            QuotationId = quotation.Id,
            FromStatus = fromStatus,
            ToStatus = quotation.Status,
            ActorUserId = actorId,
            At = now,
            Note = note ?? "Sent to customer",
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

    private static IEnumerable<QuotationLine> BuildLines(
        int quotationId,
        QuoteSubmitRequest request,
        QuoteCalculationResponse computation)
    {
        var manual = !computation.RateFound;
        var freightAmount = manual ? 0 : computation.FreightCost;
        yield return new QuotationLine
        {
            QuotationId = quotationId,
            Description = "Freight",
            Basis = manual ? ManualFreightBasis(request, computation) : computation.RateSource.ToString(),
            UnitPrice = freightAmount,
            Qty = 1,
            Amount = freightAmount,
            Currency = computation.QuoteCurrency,
        };

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

    private static string ManualFreightBasis(QuoteSubmitRequest request, QuoteCalculationResponse computation)
    {
        static string Format(decimal? value) => value?.ToString("0.###", CultureInfo.InvariantCulture) ?? "-";

        return request.Mode switch
        {
            TransportMode.Fcl => $"Manual / FCL {request.ContainerSize?.Trim().ToUpperInvariant() ?? "-"} x {request.ContainerQty} containers",
            TransportMode.Lcl => $"Manual / LCL {Format(request.Cbm)} CBM; {Format(request.WeightKg)} kg; {Format(computation.RevenueTon)} RT",
            TransportMode.Air => $"Manual / Air {Format(request.ActualWeightKg)} kg; {Format(request.VolumeCm3)} cm3; {Format(computation.ChargeableWeightKg)} kg billed",
            _ => RateSource.Manual.ToString(),
        };
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
            : throw new MissingExchangeRateException(currencyCode);
    }

    private static decimal Convert(decimal amount, string fromCurrency, string toCurrency, IReadOnlyDictionary<string, decimal> ratesToBase)
    {
        if (fromCurrency.Equals(toCurrency, StringComparison.OrdinalIgnoreCase)) return amount;
        var baseAmount = amount * RateToBase(fromCurrency, ratesToBase);
        return baseAmount / RateToBase(toCurrency, ratesToBase);
    }
}
