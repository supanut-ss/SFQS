using Freito.Domain.Enums;

namespace Freito.Domain.Entities;

/// <summary>
/// One quotation request. Created by a guest (CreatedByUserId null) or by Sale on a
/// customer's behalf. No VAT is applied anywhere in the system (confirmed with Operation) —
/// Subtotal, FreightCost + LocalChargeTotal, and FinalPrice never have a tax layer added.
/// See requirements.md §6 for the status lifecycle and technical-plan.md §2/§3.
/// </summary>
public class Quotation
{
    public int Id { get; set; }
    public string QuoteNo { get; set; } = default!; // running, unique, shown to the customer

    // Customer (guest form or Sale-entered)
    public string CustomerName { get; set; } = default!;
    public string? CustomerCompany { get; set; }
    public string CustomerEmail { get; set; } = default!;
    public string CustomerPhone { get; set; } = default!;

    // Shipment
    public int OriginPortId { get; set; }
    public int DestinationPortId { get; set; }
    public ShipmentDirection Direction { get; set; }
    public TransportMode Mode { get; set; }
    public int CargoTypeId { get; set; }
    public int Qty { get; set; }
    public string? ContainerSize { get; set; } // FCL
    public decimal? Cbm { get; set; } // LCL
    public decimal? WeightKg { get; set; } // LCL / Air
    public string IncotermCode { get; set; } = default!;
    public DateTime ReadyDate { get; set; }

    // Pricing
    public string QuoteCurrency { get; set; } = default!; // shown to the customer
    public decimal FxRateUsed { get; set; } // QuoteCurrency -> USD base, at calculation time
    public RateSource RateSource { get; set; }
    public decimal FreightCost { get; set; }
    public decimal LocalChargeTotal { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalPrice { get; set; } // editable by Sale before approval

    // Workflow
    public QuotationStatus Status { get; set; } = QuotationStatus.Draft;
    public int Version { get; set; } = 1;
    public int? CreatedByUserId { get; set; } // null = guest
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime ExpiresAt { get; set; } // created_at + 30 days, confirmed with Operation

    // Modular & Optional Sections (added by Sale/Operations as needed)
    public string? TransitTime { get; set; } // e.g. "3-5 Days"
    public string? Frequency { get; set; } // e.g. "Weekly (Wed/Fri)"
    public string? ClosingSchedule { get; set; } // e.g. "Closing export entry & VGM within Friday"
    public string? CarrierInfo { get; set; } // e.g. "MSC / Hapag" or "Cargolux"
    public string? PaymentTerms { get; set; } // e.g. "Credit 30 Days", "15 days from invoice date"
    public string? InsuranceStatus { get; set; } // e.g. "Declined", "Included", "Optional"
    public string? TermsAndConditions { get; set; } // Custom or preset disclaimer / T&C
    public string? DimensionsJson { get; set; } // JSON array of package dimension items
}
