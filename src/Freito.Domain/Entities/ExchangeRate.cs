namespace Freito.Domain.Entities;

/// <summary>
/// A snapshot of a currency's rate against the base currency (USD) at a point in time.
/// Never overwritten — a new row is inserted whenever Admin updates the rate, so historical
/// quotes can still show which rate was used (see quotation_lines snapshot design).
/// </summary>
public class ExchangeRate
{
    public int Id { get; set; }
    public string CurrencyCode { get; set; } = default!;
    public decimal RateToBase { get; set; } // 1 unit of CurrencyCode = RateToBase USD
    public DateTime EffectiveDate { get; set; }
}
