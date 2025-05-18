using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;
using DTasks.Utils;
using Microsoft.AspNetCore.Http;

namespace DTasks.AspNetCore.Infrastructure.Http;

internal static class EndpointStatusMonitorExtensions
{
    public static async Task ExecuteGetStatusAsync(this HttpContext httpContext, DAsyncId flowId, IDAsyncHeap heap)
    {
        string key = GetEndpointInfoKey(flowId);
        Option<TypedInstance<AsyncEndpointInfo>> infoOption = await heap.LoadAsync<string, TypedInstance<AsyncEndpointInfo>>(key, httpContext.RequestAborted);
            
        IResult result = infoOption
            .Map(info => info.Instance)
            .Match(Results.Ok, () => Results.NotFound());
        
        await result.ExecuteAsync(httpContext);
    }

    public static Task SetRunningAsync(this IDAsyncHeap heap, DAsyncId flowId, CancellationToken cancellationToken)
    {
        string key = GetEndpointInfoKey(flowId);
        AsyncEndpointInfo info = new()
        {
            Status = "running"
        };

        return heap.SaveAsync(key, TypedInstance.Untyped(info), cancellationToken);
    }

    public static Task SetSucceededAsync(this IDAsyncHeap heap, DAsyncId flowId, CancellationToken cancellationToken)
    {
        string key = GetEndpointInfoKey(flowId);
        AsyncEndpointInfo info = new()
        {
            Status = "succeded"
        };

        return heap.SaveAsync(key, TypedInstance.Untyped(info), cancellationToken);
    }

    public static Task SetSucceededAsync<TResult>(this IDAsyncHeap heap, DAsyncId flowId, TResult result, CancellationToken cancellationToken)
    {
        string key = GetEndpointInfoKey(flowId);
        AsyncEndpointInfo<TResult> info = new()
        {
            Status = "succeeded",
            Result = result
        };

        return heap.SaveAsync(key, TypedInstance.From(info as AsyncEndpointInfo), cancellationToken);
    }

    public static Task SetFaultedAsync(this IDAsyncHeap heap, DAsyncId flowId, Exception exception, CancellationToken cancellationToken)
    {
        string key = GetEndpointInfoKey(flowId);
        AsyncEndpointInfo info = new()
        {
            Status = "faulted"
        };

        return heap.SaveAsync(key, TypedInstance.Untyped(info), cancellationToken);
    }

    public static Task SetCanceledAsync(this IDAsyncHeap heap, DAsyncId flowId, CancellationToken cancellationToken)
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