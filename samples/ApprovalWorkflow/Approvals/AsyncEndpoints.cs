using DTasks;
using DTasks.AspNetCore;
using Microsoft.AspNetCore.Mvc;

namespace Approvals;

public class AsyncEndpoints(
    ApproverRepository repository,
    ApprovalService service)
{
    [HttpPost("approvals")]
    public async DTask<IResult> NewApproval(NewApprovalRequest request)
    {
        string? email = await repository.GetEmailByIdAsync(request.ApproverId);
        if (email is null)
            return Results.BadRequest("Invalid approver id");

        DTask<ApprovalResult> approvalTask = service.SendApprovalRequestDAsync(request.Details, email);
        DTask timeout = DTask.Delay(TimeSpan.FromDays(7));

        DTask winner = await DTask.WhenAny(timeout, approvalTask);
        ApprovalResult result = winner == timeout
            ? ApprovalResult.Reject
            : approvalTask.Result;

        return AsyncResults.Success(result);
    }
}
