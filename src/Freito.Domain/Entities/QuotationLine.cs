namespace Freito.Domain.Entities;

/// <summary>
/// A snapshot of one priced line (freight, or one local charge) at the moment a quotation
/// was calculated. This is what makes old quotes immune to later rate changes — "refresh
/// rate" compares this snapshot against the current FreightRate/LocalCharge, it never
/// mutates it. SourceRateId is nullable because a Manual-entered line has no source rate.
/// </summary>
public class QuotationLine
{
    public int Id { get; set; }
    public int QuotationId { get; set; }
    public string Description { get; set; } = default!;
    public string Basis { get; set; } = default!; // free text snapshot of the calc basis used
    public decimal UnitPrice { get; set; }
    public decimal Qty { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = default!;
    public int? SourceRateId { get; set; }
}
