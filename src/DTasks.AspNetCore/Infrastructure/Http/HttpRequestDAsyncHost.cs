using System.Diagnostics;
using DTasks.AspNetCore.Http;
using DTasks.Infrastructure;
using DTasks.Infrastructure.Marshaling;
using DTasks.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace DTasks.AspNetCore.Infrastructure.Http;

internal sealed class HttpRequestDAsyncHost(HttpContext httpContext) : AspNetCoreDAsyncHost, IAsyncHttpResultHandler
{
    private delegate Task ContinuationAction<in T>(IDAsyncContinuation continuation, DAsyncId flowId, T value, CancellationToken cancellationToken);
    
    private bool _isStarting;
    private DAsyncId _flowId;
    private TypedInstance<object> _continuationMemento;
    private TypedInstance<object>[]? _continuationMementoArray;

    protected override IServiceProvider Services => httpContext.RequestServices;

    protected override Task OnStartAsync(IDAsyncFlowStartContext context, CancellationToken cancellationToken)
    {
        _isStarting = true;
        _flowId = context.FlowId;
        
        IHeaderDictionary headers = httpContext.Request.Headers;
        if (!headers.TryGetValue(AsyncHeaders.CallbackType, out StringValues callbackTypeHeaderValues))
            return Task.CompletedTask;
        
        int callbackTypeHeaderCount = callbackTypeHeaderValues.Count;
        if (callbackTypeHeaderCount == 0)
            return Task.CompletedTask;
        
        var continuationFactory = httpContext.RequestServices.GetRequiredService<IDAsyncContinuationFactory>();
        
        if (callbackTypeHeaderCount == 1)
        {
            var callbackType = (string?)callbackTypeHeaderValues;
            if (callbackType is null)
                return OnNullCallbackHeaderAsync(cancellationToken);
            
            if (!continuationFactory.TryCreateMemento(callbackType, headers, out _continuationMemento))
                return OnUnsupportedCallbackTypeAsync(callbackType, cancellationToken);
            
            return Task.CompletedTask;
        }

        var callbackTypes = (string?[]?)callbackTypeHeaderValues;
        Debug.Assert(callbackTypes is not null, "When a header contains multiple values, we should have an array.");

        _continuationMementoArray = new TypedInstance<object>[callbackTypes.Length];
        List<string>? unsupportedCallbackTypes = null;
        for (var i = 0; i < callbackTypes.Length; i++)
        {
            string? callbackType = callbackTypes[i];
            if (callbackType is null)
                return OnNullCallbackHeaderAsync(cancellationToken);

            if (!continuationFactory.TryCreateMemento(callbackType, headers, out _continuationMementoArray[i]))
            {
                unsupportedCallbackTypes ??= new List<string>(1);
                unsupportedCallbackTypes.Add(callbackType);
            }
        }
        
        if (unsupportedCallbackTypes is not null)
            return OnUnsupportedCallbackTypesAsync(unsupportedCallbackTypes, cancellationToken);
        
        return Task.CompletedTask;
    }

    protected override Task OnSuspendAsync(CancellationToken cancellationToken)
    {
        if (!_isStarting)
            return Task.CompletedTask;

        if (_continuationMemento != default)
        {
            string callbackKey = GetContinuationKey(_flowId);
            return StateManager.Heap.SaveAsync(callbackKey, _continuationMemento, cancellationToken);
        }

        if (_continuationMementoArray is not null)
        {
            string continuationKey = GetContinuationKey(_flowId);
            TypedInstance<object> continuationMemento = AggregateDAsyncContinuation.CreateMemento(_continuationMementoArray);
            return StateManager.Heap.SaveAsync(continuationKey, continuationMemento, cancellationToken);
        }
        
        return Task.CompletedTask;
    }

    protected override Task OnSucceedAsync(IDAsyncFlowCompletionContext context, CancellationToken cancellationToken)
    {
        if (_isStarting)
        {
            Reset();
            
            // TODO: If configured, we can optionally also update the status for the status monitor
            return Results.Ok().ExecuteAsync(httpContext);
        }

        return SucceedOnResumeAsync(context, cancellationToken);
    }

    protected override Task OnSucceedAsync<TResult>(IDAsyncFlowCompletionContext context, TResult result, CancellationToken cancellationToken)
    {
        if (_isStarting)
        {
            Reset();
            
            // TODO: If configured, we can optionally also update the status for the status monitor
            var httpResult = result as IResult ?? Results.Ok(result);
            return httpResult.ExecuteAsync(httpContext);
        }

        if (result is IResult)
        {
            if (result is not IAsyncHttpResult httpResult)
                throw new InvalidOperationException("Unsupported result type returned from a d-async endpoint. Use DAsyncResults to return from a d-async method.");

            return httpResult.ExecuteAsync(this, context, cancellationToken);
        }

        return SucceedOnResumeAsync(context, result, cancellationToken);
    }

    protected override Task OnFailAsync(IDAsyncFlowCompletionContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (_isStarting)
        {
            Reset();
            
            // TODO: If configured, we can optionally also update the status for the status monitor
            var httpResult = Results.StatusCode(500); // TODO
            return httpResult.ExecuteAsync(httpContext);
        }

        return CompleteOnResumeAsync(
            context,
            exception,
            static (continuation, id, value, cancellationToken) => continuation.OnFailAsync(id, value, cancellationToken),
            cancellationToken);
    }

    protected override Task OnCancelAsync(IDAsyncFlowCompletionContext context, OperationCanceledException exception,
        CancellationToken cancellationToken)
    {
        if (_isStarting)
        {
            Reset();
            
            // TODO: If configured, we can optionally also update the status for the status monitor
            if (httpContext.RequestAborted.IsCancellationRequested)
                return Task.CompletedTask;

            throw new NotImplementedException();
        }

        return CompleteOnResumeAsync(
            context,
            default(VoidResult),
            static (continuation, id, value, cancellationToken) => continuation.OnCancelAsync(id, cancellationToken),
            cancellationToken);
    }

    Task IAsyncHttpResultHandler.SucceedAsync(IDAsyncFlowCompletionContext context,CancellationToken cancellationToken)
    {
        return SucceedOnResumeAsync(context, cancellationToken);
    }

    Task IAsyncHttpResultHandler.SucceedAsync<TResult>(IDAsyncFlowCompletionContext context,TResult result, CancellationToken cancellationToken)
    {
        return SucceedOnResumeAsync(context, result, cancellationToken);
    }

    private Task SucceedOnResumeAsync(IDAsyncFlowCompletionContext context, CancellationToken cancellationToken)
    {
        return CompleteOnResumeAsync(
            context,
            default(VoidResult),
            static (continuation, id, value, cancellationToken) => continuation.OnSucceedAsync(id, cancellationToken),
            cancellationToken);
    }

    private Task SucceedOnResumeAsync<TResult>(IDAsyncFlowCompletionContext context, TResult result, CancellationToken cancellationToken)
    {
        return CompleteOnResumeAsync(
            context,
            result,
            static (continuation, id, value, cancellationToken) => continuation.OnSucceedAsync(id, value, cancellationToken),
            cancellationToken);
    }

    private async Task CompleteOnResumeAsync<TResult>(IDAsyncFlowCompletionContext context, TResult result, ContinuationAction<TResult> continuationAction, CancellationToken cancellationToken)
    {
        DAsyncId flowId = context.FlowId;
        string continuationKey = GetContinuationKey(flowId);
        Option<TypedInstance<IDAsyncContinuationMemento>> loadResult = await StateManager.Heap.LoadAsync<string, TypedInstance<IDAsyncContinuationMemento>>(continuationKey, cancellationToken);
        
        // TODO: Check before and decide what to do should it be empty
        IDAsyncContinuation continuation = loadResult.Value.Value.Restore(Services);
        await continuationAction(continuation, flowId, result, cancellationToken);
    }

    private Task OnNullCallbackHeaderAsync(CancellationToken cancellationToken)
    {
        // TODO: Implement with proper response
        return Results.BadRequest().ExecuteAsync(httpContext);
    }

    private Task OnUnsupportedCallbackTypeAsync(string callbackType, CancellationToken cancellationToken)
    {
        // TODO: Implement with proper response
        return Results.BadRequest().ExecuteAsync(httpContext);
    }

    private Task OnUnsupportedCallbackTypesAsync(List<string> callbackTypes, CancellationToken cancellationToken)
    {
        // TODO: Implement with proper response
        return Results.BadRequest().ExecuteAsync(httpContext);
    }

    private void Reset()
    {
        _isStarting = false;
        _flowId = default;
        _continuationMemento = default;
        _continuationMementoArray = null;
    }

    private static string GetContinuationKey(DAsyncId flowId)
    {
        // TODO: Optimize and use ReadOnlySpan
        return $"{flowId}:continuation";
    }

    private readonly struct VoidResult;
}
