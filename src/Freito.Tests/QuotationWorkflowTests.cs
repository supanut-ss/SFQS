using Freito.Domain.Enums;
using Freito.Domain.Workflow;
using Xunit;

namespace Freito.Tests;

/// <summary>T8 — the mandatory Sale approval gate (requirements.md §6, AC3): a quotation must
/// never be able to reach ApprovedAndSent except from PendingSaleApproval.</summary>
public class QuotationWorkflowTests
{
    [Theory]
    [InlineData(QuotationStatus.PendingSaleApproval, true)]
    [InlineData(QuotationStatus.Draft, false)]
    [InlineData(QuotationStatus.ApprovedAndSent, false)]
    [InlineData(QuotationStatus.Rejected, false)]
    [InlineData(QuotationStatus.Confirmed, false)]
    [InlineData(QuotationStatus.Expired, false)]
    public void CanApprove_OnlyFromPendingSaleApproval(QuotationStatus status, bool expected)
    {
        Assert.Equal(expected, QuotationWorkflow.CanApprove(status));
    }

    [Theory]
    [InlineData(QuotationStatus.PendingSaleApproval, true)]
    [InlineData(QuotationStatus.ApprovedAndSent, true)] // Sale can still reject after sending
    [InlineData(QuotationStatus.Draft, false)]
    [InlineData(QuotationStatus.Rejected, false)]
    public void CanReject_FromPendingOrApproved(QuotationStatus status, bool expected)
    {
        Assert.Equal(expected, QuotationWorkflow.CanReject(status));
    }

    [Fact]
    public void CanTransition_NeverSkipsPendingSaleApproval()
    {
        Assert.False(QuotationWorkflow.CanTransition(QuotationStatus.Draft, QuotationStatus.ApprovedAndSent));
    }
}
