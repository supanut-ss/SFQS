namespace Freito.Domain.Quoting;

/// <summary>One calculated local charge line, or a NotQuotable disclaimer.</summary>
public class LocalChargeLineResult
{
    public string ChargeType { get; init; } = default!;
    public string Basis { get; init; } = default!;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = default!;

    /// <summary>True for DDP-style charges (customs duty %, at-cost, time-based) that are
    /// excluded from the total and shown as a note instead — confirmed with Operation.
    /// Amount is 0 for these; the disclaimer text is what matters.</summary>
    public bool IsNotQuotable { get; init; }
}
