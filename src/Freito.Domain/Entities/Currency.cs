namespace Freito.Domain.Entities;

/// <summary>An ISO currency. Master data. Base currency for internal rate comparison is
/// fixed as USD (confirmed with Operation) — see technical-plan.md §3.</summary>
public class Currency
{
    public string Code { get; set; } = default!; // ISO 4217, e.g. "USD"
    public string Name { get; set; } = default!;
    public int DecimalDigits { get; set; }
}
