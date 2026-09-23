using Freito.Domain.Entities;
using Freito.Domain.Enums;

namespace Freito.Domain.Quoting;

/// <summary>
/// Picks the best FreightRate for a request out of already-fetched candidates (origin/
/// destination/mode/direction filtering happens at the repository level — this class does
/// the finer matching: active flag, validity window, container size / weight bracket, and
/// cross-currency comparison). See technical-plan.md §3 "Rate resolution".
/// </summary>
public static class RateResolver
{
    /// <summary>
    /// ASSUMPTION (not yet confirmed by Operation, flagged in technical-plan.md §3): when a
    /// rate is stored as a range (PriceMin != PriceMax — real for Air), the instant quote
    /// uses PriceMax so the auto-calculated draft never under-quotes. Sale can always adjust
    /// the final price down before approval.
    /// </summary>
    public static RateResolutionResult Resolve(
        IReadOnlyList<FreightRate> candidates,
        DateTime readyDate,
        string? containerSize,
        decimal? chargeableWeightKg,
        IReadOnlyDictionary<string, decimal> exchangeRatesToBase)
    {
        var matching = candidates.Where(r =>
            r.IsActive &&
            readyDate >= r.ValidFrom && readyDate <= r.ValidTo &&
            MatchesContainerOrWeight(r, containerSize, chargeableWeightKg)
        ).ToList();

        if (matching.Count == 0)
        {
            return RateResolutionResult.NotFound();
        }

        var ranked = matching
            .Select(r => new { Rate = r, BaseCurrencyPrice = ToBase(r.PriceMax, r.CurrencyCode, exchangeRatesToBase) })
            .OrderBy(x => x.BaseCurrencyPrice)
            .ToList();

        var best = ranked[0].Rate;
        var alternatives = ranked.Skip(1).Select(x => x.Rate).ToList();
        return RateResolutionResult.Resolved(best, alternatives);
    }

    private static bool MatchesContainerOrWeight(FreightRate rate, string? containerSize, decimal? chargeableWeightKg)
    {
        if (containerSize is not null)
        {
            return string.Equals(rate.ContainerSize, containerSize, StringComparison.OrdinalIgnoreCase);
        }

        if (chargeableWeightKg is not null && rate.WeightBreakMin is not null && rate.WeightBreakMax is not null)
        {
            // Deliberately no rate rows exist above 500kg (see FreightRate doc comment) —
            // that absence is what makes this return false and fall through to NotFound.
            return chargeableWeightKg >= rate.WeightBreakMin && chargeableWeightKg <= rate.WeightBreakMax;
        }

        // LCL: no container size, no weight bracket — one flat rate per route/carrier.
        return containerSize is null && chargeableWeightKg is null;
    }

    private static decimal ToBase(decimal amount, string currencyCode, IReadOnlyDictionary<string, decimal> ratesToBase)
    {
        if (currencyCode.Equals("USD", StringComparison.OrdinalIgnoreCase))
        {
            return amount;
        }

        if (!ratesToBase.TryGetValue(currencyCode, out var rate))
        {
            throw new InvalidOperationException($"No exchange rate available for currency '{currencyCode}'.");
        }

        return amount * rate;
    }
}
