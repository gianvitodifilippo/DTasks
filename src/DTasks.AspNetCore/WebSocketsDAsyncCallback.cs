using DTasks.AspNetCore.Infrastructure.Http;

namespace DTasks.AspNetCore;

public class WebSocketsDAsyncCallback(string operationId, string connectionId, IWebSocketHandler handler) : IDAsyncCallback
{
    public async Task SucceedAsync(CancellationToken cancellationToken = default)
    {
        var message = new
        {
            operationId
        };
        await handler.SendAsync(connectionId, message, cancellationToken);
    }

    public async Task SucceedAsync<TResult>(TResult result, CancellationToken cancellationToken = default)
    {
        var message = new
        {
            operationId,
            result
        };
        await handler.SendAsync(connectionId, message, cancellationToken);
    }
}
