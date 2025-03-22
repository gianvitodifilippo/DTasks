namespace Approvals;

public class NewApprovalRequest
{
    public required ApprovalRequestDetails Details { get; set; }

    public required Guid ApproverId { get; set; }
}
