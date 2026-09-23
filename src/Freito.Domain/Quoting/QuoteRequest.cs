using Freito.Domain.Enums;

namespace Freito.Domain.Quoting;

/// <summary>Input to QuoteCalculator — already validated/resolved by the caller (port ids,
/// not raw place names).</summary>
public class QuoteRequest
{
    public required TransportMode Mode { get; init; }
    public required ShipmentDirection Direction { get; init; }
    public required int OriginPortId { get; init; }
    public required int DestinationPortId { get; init; }
    public required string IncotermCode { get; init; }
    public required DateTime ReadyDate { get; init; }

    // FCL
    public string? ContainerSize { get; init; }
    public int ContainerQty { get; init; } = 1;

    // LCL
    public decimal? Cbm { get; init; }
    public decimal? WeightKg { get; init; }

    // Air — VolumeCm3 is the shipment's total volume (sum of L×W×H×qty per piece)
    public decimal? ActualWeightKg { get; init; }
    public decimal? VolumeCm3 { get; init; }
}
