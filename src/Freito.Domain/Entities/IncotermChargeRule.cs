using Freito.Domain.Enums;

namespace Freito.Domain.Entities;

/// <summary>
/// Who is responsible for local charges at each side of the route, per Incoterm.
/// Confirmed with Operation: a quotation is always issued to one customer (per
/// ShipmentDirection) who pays 100% of what's in it — the Incoterm only determines
/// which side's charges are in scope for that customer. See technical-plan.md §2.
/// </summary>
public class IncotermChargeRule
{
    public int Id { get; set; }
    public string IncotermCode { get; set; } = default!;
    public ChargeSide ChargeSide { get; set; }
    public Payer Payer { get; set; }
}
