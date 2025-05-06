using DTasks.AspNetCore.State;
using DTasks.Infrastructure.Marshaling;
using DTasks.Utils;
using Microsoft.AspNetCore.Http;

namespace DTasks.AspNetCore.Infrastructure.Http;

internal sealed class EndpointStatusMonitor(IStateStore stateStore)
{
    public async Task ExecuteGetStatusAsync(HttpContext httpContext, DAsyncId flowId)
    {
        string key = GetEndpointInfoKey(flowId);
        Option<TypedInstance<AsyncEndpointInfo>> infoOption = await stateStore.LoadAsync<string, TypedInstance<AsyncEndpointInfo>>(key, httpContext.RequestAborted);
            
        IResult result = infoOption
            .Map(info => info.Instance)
            .Fold(Results.Ok, () => Results.NotFound());
        
        await result.ExecuteAsync(httpContext);
    }

    public Task SetRunningAsync(DAsyncId flowId, CancellationToken cancellationToken)
    {
        string key = GetEndpointInfoKey(flowId);
        AsyncEndpointInfo info = new()
        {
            Status = "running"
        };

        return stateStore.SaveAsync(key, TypedInstance.Untyped(info), cancellationToken);
    }

    public Task SetSucceededAsync(DAsyncId flowId, CancellationToken cancellationToken)
    {
        string key = GetEndpointInfoKey(flowId);
        AsyncEndpointInfo info = new()
        {
            Status = "succeded"
        };

        return stateStore.SaveAsync(key, TypedInstance.Untyped(info), cancellationToken);
    }

    public Task SetSucceededAsync<TResult>(DAsyncId flowId, TResult result, CancellationToken cancellationToken)
    {
        string key = GetEndpointInfoKey(flowId);
        AsyncEndpointInfo<TResult> info = new()
        {
            Status = "succeeded",
            Result = result
        };

        return stateStore.SaveAsync(key, TypedInstance.Untyped(info), cancellationToken);
    }

    public Task SetFaultedAsync(DAsyncId flowId, Exception exception, CancellationToken cancellationToken)
    {
        string key = GetEndpointInfoKey(flowId);
        AsyncEndpointInfo info = new()
        {
            Status = "faulted"
        };

        return stateStore.SaveAsync(key, TypedInstance.Untyped(info), cancellationToken);
    }

    public Task SetCanceledAsync(DAsyncId flowId, CancellationToken cancellationToken)
    {
        string key = GetEndpointInfoKey(flowId);
        AsyncEndpointInfo info = new()
        {
            Status = "canceled"
        };

        return stateStore.SaveAsync(key, TypedInstance.Untyped(info), cancellationToken);
    }

    private static string GetEndpointInfoKey(DAsyncId flowId)
    {
        // TODO: Optimize and use ReadOnlySpan
        return $"{flowId}:info";
    }
}