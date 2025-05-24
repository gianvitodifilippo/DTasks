using DTasks.AspNetCore.Execution;
using DTasks.AspNetCore.Metadata;

namespace Approvals;

[ResumptionEndpoints]
public static class ResumptionEndpoints
{
    public static readonly ResumptionEndpoint<ApprovalResult> Approval = "approvals/resume/{operationId}";
}