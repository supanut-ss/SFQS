namespace Freito.Domain.Entities;

/// <summary>
/// Generic audit trail for master-data/rate changes, so Operation's frequent rate updates
/// can be traced (who changed what, when) — see requirements.md §7.
/// </summary>
public class AuditLog
{
    public int Id { get; set; }
    public string Entity { get; set; } = default!; // e.g. "FreightRate"
    public int EntityId { get; set; }
    public string Action { get; set; } = default!; // "Created" / "Updated" / "Deleted"
    public int ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
}
