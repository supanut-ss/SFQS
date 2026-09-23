using Freito.Domain.Enums;

namespace Freito.Domain.Entities;

/// <summary>
/// A local charge line (THC, D/O, CFS, ...) at one port, for one mode/direction. Real
/// charges are often quoted as a range (AmountMin/AmountMax) rather than a fixed number,
/// and some have a floor (MinimumCharge) below which the per-unit calculation never drops —
/// both confirmed from real Ryder World Transport quotes, see operation-worksheet.md §4.
/// </summary>
public class LocalCharge
{
    public int Id { get; set; }
    public int PortId { get; set; }
    public ShipmentDirection Direction { get; set; }
    public TransportMode Mode { get; set; }
    public string ChargeType { get; set; } = default!; // e.g. "THC", "D/O", "CFS"
    public ChargeCalcBasis CalcBasis { get; set; }
    public decimal AmountMin { get; set; }
    public decimal AmountMax { get; set; } // equal to AmountMin when the price is fixed
    public decimal? MinimumCharge { get; set; }
    public string CurrencyCode { get; set; } = default!;
    public ChargeSide ChargeSide { get; set; }
}
