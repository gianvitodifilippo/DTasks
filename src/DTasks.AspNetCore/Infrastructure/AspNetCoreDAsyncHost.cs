using DTasks.AspNetCore.Infrastructure.Http;
using DTasks.Infrastructure;
using DTasks.Infrastructure.Marshaling;
using DTasks.Utils;
using Microsoft.AspNetCore.Http;

namespace DTasks.AspNetCore.Infrastructure;

public abstract partial class AspNetCoreDAsyncHost
{
    private delegate Task ContinuationAction<in T>(IDAsyncContinuation continuation, DAsyncId flowId, T value, CancellationToken cancellationToken);

    private bool _isOnStart;
    private TypedInstance<object> _continuationMemento;
    private TypedInstance<object>[]? _continuationMementoArray;

    private protected AspNetCoreDAsyncHost()
    {
    }
    
    protected abstract IServiceProvider Services { get; }

    protected DAsyncId FlowId { get; private set; }

    public ValueTask StartAsync(IDAsyncRunnable runnable, CancellationToken cancellationToken = default)
    {
        return DAsyncFlow.StartFlowAsync(this, runnable, cancellationToken);
    }
    
    public ValueTask ResumeAsync(DAsyncId id, CancellationToken cancellationToken = default)
    {
        return DAsyncFlow.ResumeFlowAsync(this, id, cancellationToken);
    }

    public ValueTask ResumeAsync<TResult>(DAsyncId id, TResult result, CancellationToken cancellationToken = default)
    {
        return DAsyncFlow.ResumeFlowAsync(this, id, result, cancellationToken);
    }

    public ValueTask ResumeAsync(DAsyncId id, Exception exception, CancellationToken cancellationToken = default)
    {
        return DAsyncFlow.ResumeFlowAsync(this, id, exception, cancellationToken);
    }

    protected virtual Task OnStartCoreAsync(IDAsyncFlowStartContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected sealed override Task OnStartAsync(IDAsyncFlowStartContext context, CancellationToken cancellationToken)
    {
        _isOnStart = true;
        FlowId = context.FlowId;

        return OnStartCoreAsync(context, cancellationToken);
    }

    protected override Task OnSuspendAsync(CancellationToken cancellationToken)
    {
        if (!_isOnStart)
            return Task.CompletedTask;

        if (_continuationMemento != default)
        {
            string callbackKey = GetContinuationKey(FlowId);
            return StateManager.Heap.SaveAsync(callbackKey, _continuationMemento, cancellationToken);
        }

        if (_continuationMementoArray is not null)
        {
            string continuationKey = GetContinuationKey(FlowId);
            TypedInstance<object> continuationMemento = AggregateDAsyncContinuation.CreateMemento(_continuationMementoArray);
            return StateManager.Heap.SaveAsync(continuationKey, continuationMemento, cancellationToken);
        }
        
        return Task.CompletedTask;
    }


    protected override Task OnSucceedAsync(IDAsyncFlowCompletionContext context, CancellationToken cancellationToken)
    {
        if (_isOnStart)
        {
            Reset();
            
            // TODO: If configured, we can optionally also update the status for the status monitor
            return SucceedOnStartAsync(context, cancellationToken);
        }

        return SucceedOnResumeAsync(context, cancellationToken);
    }

    protected override Task OnSucceedAsync<TResult>(IDAsyncFlowCompletionContext context, TResult result, CancellationToken cancellationToken)
    {
        if (_isOnStart)
        {
            Reset();
            
            // TODO: If configured, we can optionally also update the status for the status monitor
            return SucceedOnStartAsync(context, result, cancellationToken);
        }

        return SucceedOnResumeAsync(context, result, cancellationToken);
    }

    protected override Task OnFailAsync(IDAsyncFlowCompletionContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (_isOnStart)
        {
            Reset();
            
            // TODO: If configured, we can optionally also update the status for the status monitor
            return FailOnStartAsync(context, exception, cancellationToken);
        }

        return FailOnResumeAsync(context, exception, cancellationToken);
    }

    protected override Task OnCancelAsync(IDAsyncFlowCompletionContext context, OperationCanceledException exception,
        CancellationToken cancellationToken)
    {
        if (_isOnStart)
        {
            Reset();
            
            // TODO: If configured, we can optionally also update the status for the status monitor
            return CancelOnStartAsync(context, cancellationToken);
        }

        return CancelOnResumeAsync(context, cancellationToken);
    }
    protected void SetContinuation(TypedInstance<object> memento)
    {
        _continuationMemento = memento;
    }

    protected void SetContinuation(TypedInstance<object>[] mementoArray)
    {
        _continuationMementoArray = mementoArray;
    }

    protected virtual Task SuspendOnStartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    
    protected virtual Task SucceedOnStartAsync(IDAsyncFlowCompletionContext context, CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual Task SucceedOnStartAsync<TResult>(IDAsyncFlowCompletionContext context, TResult result, CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual Task FailOnStartAsync(IDAsyncFlowCompletionContext context, Exception exception, CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual Task CancelOnStartAsync(IDAsyncFlowCompletionContext context, CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual Task SucceedOnResumeAsync(IDAsyncFlowCompletionContext context, CancellationToken cancellationToken)
    {
        return CompleteOnResumeAsync(
            context,
            default(VoidResult),
            static (continuation, id, value, cancellationToken) => continuation.OnSucceedAsync(id, cancellationToken),
            cancellationToken);
    }

    protected virtual Task SucceedOnResumeAsync<TResult>(IDAsyncFlowCompletionContext context, TResult result, CancellationToken cancellationToken)
    {
        return CompleteOnResumeAsync(
            context,
            result,
            static (continuation, id, value, cancellationToken) => continuation.OnSucceedAsync(id, value, cancellationToken),
            cancellationToken);
    }

    protected virtual Task FailOnResumeAsync(IDAsyncFlowCompletionContext context, Exception exception, CancellationToken cancellationToken)
    {
        return CompleteOnResumeAsync(
            context,
            exception,
            static (continuation, id, value, cancellationToken) => continuation.OnFailAsync(id, value, cancellationToken),
            cancellationToken);
    }

    protected virtual Task CancelOnResumeAsync(IDAsyncFlowCompletionContext context, CancellationToken cancellationToken)
    {
        return CompleteOnResumeAsync(
            context,
            default(VoidResult),
            static (continuation, id, value, cancellationToken) => continuation.OnCancelAsync(id, cancellationToken),
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

    private void Reset()
    {
        _isOnStart = false;
        FlowId = default;
        _continuationMemento = default;
        _continuationMementoArray = null;
    }

    private static string GetContinuationKey(DAsyncId flowId)
    {
        // TODO: Optimize and use ReadOnlySpan
        return $"{flowId}:continuation";
    }

    public static AspNetCoreDAsyncHost CreateHttpHost(HttpContext httpContext, string monitorActionName)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(monitorActionName);

        return new HttpRequestDAsyncHost(httpContext, monitorActionName);
    }

    private readonly struct VoidResult;
}
