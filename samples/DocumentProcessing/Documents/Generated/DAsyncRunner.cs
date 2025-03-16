using DTasks;
using DTasks.AspNetCore;

namespace Documents.Generated;

public class DAsyncRunner(
    AsyncEndpoints endpoints,
    AspNetCoreDAsyncHost host,
    IWebSocketHandler handler)
{
    public async DTask<IResult> ProcessDocument(string operationId, string documentId)
    {
        IResult result = await endpoints.ProcessDocument(documentId);
        host.SetCallback(operationId, null);
        return result;
    }

    public async DTask<IResult> ProcessDocument_Websockets(string operationId, string connectionId, string documentId)
    {
        IResult result = await endpoints.ProcessDocument(documentId);
        host.SetCallback(operationId, new WebSocketsDAsyncCallback(operationId, connectionId, handler));
        return result;
    }
}
