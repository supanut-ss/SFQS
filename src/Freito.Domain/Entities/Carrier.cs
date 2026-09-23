using Freito.Domain.Enums;

namespace Freito.Domain.Entities;

/// <summary>A shipping line or airline. Master data, managed by Admin.</summary>
public class Carrier
{
    public int Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public CarrierType Type { get; set; }
}
