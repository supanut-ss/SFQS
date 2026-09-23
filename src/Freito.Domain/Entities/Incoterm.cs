namespace Freito.Domain.Entities;

/// <summary>Fixed master data: the 11 Incoterms 2020 terms. Seeded once, not user-editable.</summary>
public class Incoterm
{
    public string Code { get; set; } = default!; // e.g. "FOB"
    public string Name { get; set; } = default!;
    public string RiskTransferPoint { get; set; } = default!;
    public bool SellerPaysFreight { get; set; }
}
