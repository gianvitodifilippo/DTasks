using DTasks.Utils;

namespace DTasks.AspNetCore.Infrastructure.Http;

internal interface IAsyncEndpointMonitor
{
    Task<Option<AsyncEndpointInfo>> GetEndpointInfoAsync(DAsyncId flowId, CancellationToken cancellationToken);

    Task SetRunningAsync(DAsyncId flowId, CancellationToken cancellationToken);

    Task SetSucceededAsync(DAsyncId flowId, CancellationToken cancellationToken);

    Task SetSucceededAsync<TResult>(DAsyncId flowId, TResult result, CancellationToken cancellationToken);

    Task SetFaultedAsync(DAsyncId flowId, Exception exception, CancellationToken cancellationToken);

    Task SetCanceledAsync(DAsyncId flowId, CancellationToken cancellationToken);
}