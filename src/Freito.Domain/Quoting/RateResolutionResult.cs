using Freito.Domain.Entities;
using Freito.Domain.Enums;

namespace Freito.Domain.Quoting;

/// <summary>Result of trying to find a freight rate for a request.</summary>
public class RateResolutionResult
{
    public bool Found { get; init; }
    public RateSource Source { get; init; }
    public FreightRate? Rate { get; init; }

    /// <summary>Other candidate rates that matched but weren't the cheapest — offered to
    /// Sale as alternative carriers, per technical-plan.md §3 step 3.</summary>
    public IReadOnlyList<FreightRate> Alternatives { get; init; } = Array.Empty<FreightRate>();

    public static RateResolutionResult NotFound() => new() { Found = false, Source = RateSource.Manual };

    public static RateResolutionResult Resolved(FreightRate rate, IReadOnlyList<FreightRate> alternatives) =>
        new() { Found = true, Source = RateSource.System, Rate = rate, Alternatives = alternatives };
}
