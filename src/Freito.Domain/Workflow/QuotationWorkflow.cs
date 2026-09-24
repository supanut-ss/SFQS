using Freito.Domain.Enums;

namespace Freito.Domain.Workflow;

/// <summary>
/// The mandatory Sale approval gate (requirements.md §6, AC3): a quotation can never reach
/// ApprovedAndSent without passing through PendingSaleApproval, no matter how the price was
/// calculated. This is the single source of truth for which transitions are legal — both the
/// API layer and its tests should go through this rather than checking Status inline, so the
/// gate can't accidentally be bypassed by a new code path forgetting the check.
/// </summary>
public static class QuotationWorkflow
{
    private static readonly IReadOnlyDictionary<QuotationStatus, QuotationStatus[]> AllowedTransitions =
        new Dictionary<QuotationStatus, QuotationStatus[]>
        {
            [QuotationStatus.Draft] = [QuotationStatus.PendingSaleApproval],
            [QuotationStatus.PendingSaleApproval] = [QuotationStatus.ApprovedAndSent, QuotationStatus.Rejected],
            [QuotationStatus.ApprovedAndSent] = [QuotationStatus.Confirmed, QuotationStatus.Rejected, QuotationStatus.Expired],
            [QuotationStatus.Rejected] = [],
            [QuotationStatus.Confirmed] = [],
            [QuotationStatus.Expired] = [],
        };

    public static bool CanTransition(QuotationStatus from, QuotationStatus to) =>
        AllowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public static bool CanApprove(QuotationStatus status) => CanTransition(status, QuotationStatus.ApprovedAndSent);

    public static bool CanReject(QuotationStatus status) => CanTransition(status, QuotationStatus.Rejected);
}
