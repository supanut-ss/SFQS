using System.ComponentModel.DataAnnotations;
using Freito.Domain.Enums;

namespace Freito.Api.Models;

/// <summary>Shipment details shared by calculate (guest, no persistence) and submit (guest,
/// creates a Draft). Field requirements per mode are checked in QuotationService, not here,
/// since they depend on Mode's value (DataAnnotations can't easily express that).</summary>
public record QuoteShipmentRequest
{
    [Required, EnumDataType(typeof(TransportMode))]
    public TransportMode? Mode { get; init; }

    [Required, EnumDataType(typeof(ShipmentDirection))]
    public ShipmentDirection? Direction { get; init; }

    [Range(1, int.MaxValue)]
    public int OriginPortId { get; init; }

    [Range(1, int.MaxValue)]
    public int DestinationPortId { get; init; }

    [Required, RegularExpression("^[A-Za-z]{3}$")]
    public string IncotermCode { get; init; } = string.Empty;

    [Required]
    public DateTime? ReadyDate { get; init; }

    // FCL
    [StringLength(16)]
    public string? ContainerSize { get; init; }

    [Range(1, 999)]
    public int ContainerQty { get; init; } = 1;

    // LCL
    [Range(typeof(decimal), "0.001", "999999999.999")]
    public decimal? Cbm { get; init; }

    [Range(typeof(decimal), "0.001", "999999999.999")]
    public decimal? WeightKg { get; init; }

    // Air
    [Range(typeof(decimal), "0.001", "999999999.999")]
    public decimal? ActualWeightKg { get; init; }

    [Range(typeof(decimal), "0", "999999999999")]
    public decimal? VolumeCm3 { get; init; }
}

public sealed record QuoteSubmitRequest : QuoteShipmentRequest
{
    [Required, StringLength(160, MinimumLength = 1)]
    public string CustomerName { get; init; } = string.Empty;

    [StringLength(160)]
    public string? CustomerCompany { get; init; }

    [Required, EmailAddress, StringLength(254)]
    public string CustomerEmail { get; init; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 1)]
    public string CustomerPhone { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CargoTypeId { get; init; }
}

public sealed record LocalChargeLineDto(string ChargeType, string Basis, decimal Amount, string Currency, bool IsNotQuotable);

public sealed record FreightAlternativeDto(int CarrierId, decimal Price, string Currency);

/// <summary>Response for both calculate and submit — submit adds QuoteId/QuoteNo/ExpiresAt
/// on top of the same pricing breakdown so the guest form can render one result shape.</summary>
public sealed record QuoteCalculationResponse
{
    public int? QuoteId { get; init; }
    public string? QuoteNo { get; init; }
    public DateTime? ExpiresAt { get; init; }

    public bool RateFound { get; init; }
    public RateSource RateSource { get; init; }
    public string QuoteCurrency { get; init; } = default!;
    public decimal FxRateUsed { get; init; }
    public decimal FreightCost { get; init; }
    public decimal LocalChargeTotal { get; init; }
    public decimal Subtotal { get; init; }
    public decimal? ChargeableWeightKg { get; init; }
    public decimal? RevenueTon { get; init; }
    public IReadOnlyList<LocalChargeLineDto> LocalChargeLines { get; init; } = [];
    public IReadOnlyList<string> NotQuotableNotes { get; init; } = [];
    public IReadOnlyList<FreightAlternativeDto> Alternatives { get; init; } = [];
    public string? NoRateFoundReason { get; init; }
}

public sealed record RefreshRateLineDelta(
    string ChargeType,
    decimal PreviousAmount,
    decimal CurrentAmount,
    decimal Delta);

public sealed class QuoteLineItemDto
{
    public int? Id { get; init; }
    [Required, StringLength(200)]
    public string Description { get; init; } = string.Empty;
    [StringLength(80)]
    public string Basis { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public decimal Qty { get; init; } = 1;
    [Range(0, 99999999999999.9999)]
    public decimal Amount { get; init; }
    [StringLength(3)]
    public string Currency { get; init; } = string.Empty;
}

public sealed class UpdateQuoteLinesRequest
{
    public List<QuoteLineItemDto> Lines { get; init; } = [];
    public decimal? FinalPrice { get; init; }
    [StringLength(1000)]
    public string? Note { get; init; }

    // Customer edits
    [StringLength(160)]
    public string? CustomerName { get; init; }
    [StringLength(160)]
    public string? CustomerCompany { get; init; }
    [EmailAddress, StringLength(254)]
    public string? CustomerEmail { get; init; }
    [StringLength(50)]
    public string? CustomerPhone { get; init; }

    // Shipment edits
    public int? OriginPortId { get; init; }
    public int? DestinationPortId { get; init; }
    public ShipmentDirection? Direction { get; init; }
    public TransportMode? Mode { get; init; }
    public int? CargoTypeId { get; init; }
    public int? Qty { get; init; }
    [StringLength(16)]
    public string? ContainerSize { get; init; }
    public decimal? Cbm { get; init; }
    public decimal? WeightKg { get; init; }
    [StringLength(3)]
    public string? IncotermCode { get; init; }
    public DateTime? ReadyDate { get; init; }

    // Modular optional sections
    [StringLength(100)]
    public string? TransitTime { get; init; }
    [StringLength(100)]
    public string? Frequency { get; init; }
    [StringLength(200)]
    public string? ClosingSchedule { get; init; }
    [StringLength(150)]
    public string? CarrierInfo { get; init; }
    [StringLength(150)]
    public string? PaymentTerms { get; init; }
    [StringLength(100)]
    public string? InsuranceStatus { get; init; }
    [StringLength(4000)]
    public string? TermsAndConditions { get; init; }
    [StringLength(4000)]
    public string? DimensionsJson { get; init; }
}

public sealed class ApproveQuoteRequest
{
    [Range(typeof(decimal), "0", "99999999999999.9999")]
    public decimal? FinalPrice { get; init; } // omit to keep the computed Subtotal as-is

    [StringLength(1000)]
    public string? Note { get; init; }

    public List<QuoteLineItemDto>? Lines { get; init; }

    // Customer edits
    [StringLength(160)]
    public string? CustomerName { get; init; }
    [StringLength(160)]
    public string? CustomerCompany { get; init; }
    [EmailAddress, StringLength(254)]
    public string? CustomerEmail { get; init; }
    [StringLength(50)]
    public string? CustomerPhone { get; init; }

    // Shipment edits
    public int? OriginPortId { get; init; }
    public int? DestinationPortId { get; init; }
    public ShipmentDirection? Direction { get; init; }
    public TransportMode? Mode { get; init; }
    public int? CargoTypeId { get; init; }
    public int? Qty { get; init; }
    [StringLength(16)]
    public string? ContainerSize { get; init; }
    public decimal? Cbm { get; init; }
    public decimal? WeightKg { get; init; }
    [StringLength(3)]
    public string? IncotermCode { get; init; }
    public DateTime? ReadyDate { get; init; }

    // Modular optional sections
    [StringLength(100)]
    public string? TransitTime { get; init; }
    [StringLength(100)]
    public string? Frequency { get; init; }
    [StringLength(200)]
    public string? ClosingSchedule { get; init; }
    [StringLength(150)]
    public string? CarrierInfo { get; init; }
    [StringLength(150)]
    public string? PaymentTerms { get; init; }
    [StringLength(100)]
    public string? InsuranceStatus { get; init; }
    [StringLength(4000)]
    public string? TermsAndConditions { get; init; }
    [StringLength(4000)]
    public string? DimensionsJson { get; init; }
}

public sealed class RejectQuoteRequest
{
    [Required, StringLength(1000, MinimumLength = 1)]
    public string Note { get; init; } = string.Empty;
}

public sealed class SendQuoteRequest
{
    [StringLength(1000)]
    public string? Note { get; init; }
}

public sealed record RefreshRateResponse(
    decimal PreviousFreightCost,
    decimal CurrentFreightCost,
    decimal FreightDelta,
    decimal PreviousSubtotal,
    decimal CurrentSubtotal,
    decimal SubtotalDelta,
    string Currency,
    bool CurrentRateFound,
    IReadOnlyList<RefreshRateLineDelta> LocalChargeDeltas);
