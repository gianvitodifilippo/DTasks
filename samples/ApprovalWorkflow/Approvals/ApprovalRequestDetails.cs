namespace Approvals;

public class ApprovalRequestDetails
{
    public required string Requestor { get; set; }

    public required string ApprovalCategory { get; set; }

    public required Uri ResourceUri { get; set; }
}
