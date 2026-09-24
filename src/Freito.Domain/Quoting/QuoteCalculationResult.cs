using Freito.Domain.Enums;

namespace Freito.Domain.Quoting;

/// <summary>
/// Output of QuoteCalculator. Subtotal is Freight + LocalCharge only — no VAT layer exists
/// anywhere in this system (confirmed with Operation). NotQuotableNotes are shown as a
/// disclaimer under the total, never added into Subtotal.
/// </summary>
public class QuoteCalculationResult
{
    public bool RateFound { get; init; }
    public RateSource RateSource { get; init; }

    /// <summary>Currency FreightCost is denominated in — the resolved FreightRate's own
    /// currency. Null when no rate was found (FreightCost is 0 and Sale must price manually).</summary>
    public string? FreightCurrency { get; init; }
    public decimal FreightCost { get; init; }
    public decimal LocalChargeTotal { get; init; }
    public decimal Subtotal { get; init; }
    public decimal? ChargeableWeightKg { get; init; }
    public decimal? RevenueTon { get; init; }
    public IReadOnlyList<LocalChargeLineResult> LocalChargeLines { get; init; } = Array.Empty<LocalChargeLineResult>();
    public IReadOnlyList<string> NotQuotableNotes { get; init; } = Array.Empty<string>();
    public string? NoRateFoundReason { get; init; }
}
