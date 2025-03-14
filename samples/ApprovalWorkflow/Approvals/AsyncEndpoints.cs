using DTasks;
using DTasks.AspNetCore;
using Microsoft.AspNetCore.Mvc;

namespace Approvals;

public class AsyncEndpoints(
    ApproverRepository repository,
    ApprovalService service)
{
    [HttpPost]
    public async DTask<IResult> StartApproval(StartApprovalRequest request)
    {
        string? email = await repository.GetEmailByIdAsync(request.ApproverId);
        if (email is null)
            return Results.BadRequest("Invalid approver id");

        DTask<ApprovalResult> approvalTask = service.SendApprovalRequestDAsync(request.Details, email);
        DTask timeout = DTask.Delay(TimeSpan.FromSeconds(30));

        DTask winner = await DTask.WhenAny(timeout, approvalTask);
        if (winner == timeout)
            return AsyncResults.Success(ApprovalResult.Reject);

        return AsyncResults.Success(approvalTask.Result);
    }
}
