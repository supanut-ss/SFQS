using Freito.Domain.Enums;

namespace Freito.Domain.Entities;

/// <summary>Audit trail of every status transition a quotation went through.</summary>
public class QuotationStatusHistory
{
    public int Id { get; set; }
    public int QuotationId { get; set; }
    public QuotationStatus FromStatus { get; set; }
    public QuotationStatus ToStatus { get; set; }
    public int? ActorUserId { get; set; } // null = system/guest action
    public string? Note { get; set; }
    public DateTime At { get; set; }
}
