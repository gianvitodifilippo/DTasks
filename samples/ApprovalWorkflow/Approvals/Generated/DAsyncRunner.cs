using DTasks;
using DTasks.AspNetCore;

namespace Approvals.Generated;

public class DAsyncRunner(
    AsyncEndpoints endpoints,
    AspNetCoreDAsyncHost host,
    IHttpClientFactory httpClientFactory)
{
    public async DTask<IResult> StartApproval(string operationId, StartApprovalRequest request)
    {
        IResult result = await endpoints.StartApproval(request);
        host.SetCallback(operationId, null);
        return result;
    }

    public async DTask<IResult> StartApproval_Webhook(string operationId, Uri callbackAddress, StartApprovalRequest request)
    {
        IResult result = await endpoints.StartApproval(request);
        host.SetCallback(operationId, new WebhookDAsyncCallback(httpClientFactory, callbackAddress, operationId));
        return result;
    }
}
