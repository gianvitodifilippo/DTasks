using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;
using DTasks.Utils;

namespace DTasks.AspNetCore.Infrastructure.Http;

internal sealed class AsyncEndpointMonitor(IDAsyncHeap heap) : IAsyncEndpointMonitor
{
    public async Task<Option<AsyncEndpointInfo>> GetEndpointInfoAsync(DAsyncId flowId, CancellationToken cancellationToken)
    {
        string key = GetEndpointInfoKey(flowId);
        Option<TypedInstance<AsyncEndpointInfo>> infoOption = await heap.LoadAsync<string, TypedInstance<AsyncEndpointInfo>>(key, cancellationToken);

        return infoOption.Map(info => info.Instance);
    }

    public Task SetRunningAsync(DAsyncId flowId, CancellationToken cancellationToken)
    {
        string key = GetEndpointInfoKey(flowId);
        AsyncEndpointInfo info = new()
        {
            Status = "running"
        };

        return heap.SaveAsync(key, TypedInstance.Untyped(info), cancellationToken);
    }

    public Task SetSucceededAsync(DAsyncId flowId, CancellationToken cancellationToken)
    {
        string key = GetEndpointInfoKey(flowId);
        AsyncEndpointInfo info = new()
        {
            Status = "succeded"
        };

        return heap.SaveAsync(key, TypedInstance.Untyped(info), cancellationToken);
    }

    public Task SetSucceededAsync<TResult>(DAsyncId flowId, TResult result, CancellationToken cancellationToken)
    {
        string key = GetEndpointInfoKey(flowId);
        AsyncEndpointInfo<TResult> info = new()
        {
            Status = "succeeded",
            Result = result
        };

        return heap.SaveAsync(key, TypedInstance.Untyped(info), cancellationToken);
    }

    public Task SetFaultedAsync(DAsyncId flowId, Exception exception, CancellationToken cancellationToken)
    {
        string key = GetEndpointInfoKey(flowId);
        AsyncEndpointInfo info = new()
        {
            Status = "faulted"
        };

        return heap.SaveAsync(key, TypedInstance.Untyped(info), cancellationToken);
    }

    public Task SetCanceledAsync(DAsyncId flowId, CancellationToken cancellationToken)
    {
        string key = GetEndpointInfoKey(flowId);
        AsyncEndpointInfo info = new()
        {
            Status = "canceled"
        };

        return heap.SaveAsync(key, TypedInstance.Untyped(info), cancellationToken);
    }

    private static string GetEndpointInfoKey(DAsyncId flowId)
    {
        // TODO: Optimize and use ReadOnlySpan
        return $"{flowId}:info";
    }
}