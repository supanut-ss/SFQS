namespace Freito.Domain.Quoting;

public enum RecommendedMode
{
    Lcl,
    Fcl,
}

public class BreakevenResult
{
    public decimal LclTotal { get; init; }
    public decimal FclTotal { get; init; }
    public RecommendedMode Recommended { get; init; }
}

/// <summary>
/// LCL vs FCL comparison. Formula corrected from the original requirements.md draft, which
/// incorrectly multiplied the whole LCL side (freight + local) by CBM — local charges are
/// per-shipment, not per-CBM, so only the freight rate should be multiplied. See
/// technical-plan.md §3.
/// </summary>
public static class BreakevenCalculator
{
    /// <summary>
    /// lclFreightRatePerCbmOrTon: the LCL freight rate (already per revenue ton / CBM as
    /// appropriate) — NOT the total freight cost. revenueTonOrCbm: the multiplier already
    /// used to get the LCL freight cost, passed separately so this stays a pure comparison
    /// that doesn't recompute chargeable quantities itself.
    /// </summary>
    public static BreakevenResult Compare(
        decimal lclFreightCost, decimal lclLocalChargeTotal,
        decimal fclFreightCost, decimal fclLocalChargeTotal)
    {
        var lclTotal = lclFreightCost + lclLocalChargeTotal;
        var fclTotal = fclFreightCost + fclLocalChargeTotal;

        return new BreakevenResult
        {
            LclTotal = lclTotal,
            FclTotal = fclTotal,
            Recommended = lclTotal <= fclTotal ? RecommendedMode.Lcl : RecommendedMode.Fcl,
        };
    }
}
