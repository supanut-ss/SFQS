using Freito.Domain.Enums;

namespace Freito.Domain.Entities;

/// <summary>
/// A freight rate for one route + mode + carrier, valid for a date range. Never overwritten
/// in place — updating a rate inserts a new row with a new validity window, so quotes already
/// snapshotted (see QuotationLine) are unaffected. See technical-plan.md §2/§3.
/// </summary>
public class FreightRate
{
    public int Id { get; set; }
    public int OriginPortId { get; set; }
    public int DestinationPortId { get; set; }
    public TransportMode Mode { get; set; }
    public ShipmentDirection Direction { get; set; }
    public int CarrierId { get; set; }

    /// <summary>FCL only, e.g. "20", "40", "40HQ".</summary>
    public string? ContainerSize { get; set; }

    /// <summary>
    /// Air only, kg. The lower bound is inclusive and the upper bound exclusive, except
    /// 500kg is included in the final bracket. A row with WeightBreakMin=0, WeightBreakMax=50
    /// represents the minimum chargeable weight bracket (anything under 50kg is billed as
    /// 50kg) — confirmed with Operation, see technical-plan.md §3. No row is seeded above
    /// 500kg on purpose: rate resolution should return NoRateFound there so Sale must enter
    /// the price manually.
    /// </summary>
    public decimal? WeightBreakMin { get; set; }
    public decimal? WeightBreakMax { get; set; }

    /// <summary>
    /// A range, not a single price — real Air rates are quoted as e.g. $2.95-$3.10/kg.
    /// Equal for FCL/LCL where the rate is flat.
    /// </summary>
    public decimal PriceMin { get; set; }
    public decimal PriceMax { get; set; }

    public string CurrencyCode { get; set; } = default!;
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
}
