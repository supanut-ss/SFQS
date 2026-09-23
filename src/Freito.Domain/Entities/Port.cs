using Freito.Domain.Enums;

namespace Freito.Domain.Entities;

/// <summary>A sea port or airport. Master data, managed by Admin.</summary>
public class Port
{
    public int Id { get; set; }
    public string Code { get; set; } = default!; // UN/LOCODE, e.g. "THBKK"
    public string Name { get; set; } = default!;
    public string City { get; set; } = default!;
    public string Country { get; set; } = default!;
    public PortType Type { get; set; }
}
