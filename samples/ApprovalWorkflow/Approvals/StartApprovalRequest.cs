namespace Approvals;

public class StartApprovalRequest
{
    public required ApprovalRequestDetails Details { get; set; }

    public required Guid ApproverId { get; set; }
}
