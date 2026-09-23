namespace Freito.Domain.Entities;

/// <summary>A cargo category, used to flag dangerous/prohibited goods. Master data.</summary>
public class CargoType
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public bool IsDangerous { get; set; }
    public bool IsProhibited { get; set; }
}
