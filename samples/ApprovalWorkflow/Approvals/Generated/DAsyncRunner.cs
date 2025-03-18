using DTasks;
using DTasks.AspNetCore;

namespace Approvals.Generated;

public class DAsyncRunner(
    AsyncEndpoints endpoints,
    AspNetCoreDAsyncHost host,
    IHttpClientFactory httpClientFactory)
{
    public async DTask<IResult> NewApproval(string operationId, NewApprovalRequest request)
    {
        IResult result = await endpoints.NewApproval(request);
        host.SetCallback(operationId, null);
        return result;
    }

    public async DTask<IResult> NewApproval_Webhook(string operationId, Uri callbackAddress, NewApprovalRequest request)
    {
        IResult result = await endpoints.NewApproval(request);
        host.SetCallback(operationId, new WebhookDAsyncCallback(httpClientFactory, callbackAddress, operationId));
        return result;
    }
}
