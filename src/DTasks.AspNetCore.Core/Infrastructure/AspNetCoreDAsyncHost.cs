using System.Diagnostics;
using DTasks.AspNetCore.Infrastructure.Http;
using DTasks.Extensions.DependencyInjection.Infrastructure;
using DTasks.Infrastructure;
using DTasks.Infrastructure.Marshaling;
using DTasks.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.AspNetCore.Infrastructure;

public abstract class AspNetCoreDAsyncHost : ServicedDAsyncHost
{
    private delegate Task ContinuationAction<in T>(IDAsyncContinuation continuation, DAsyncId flowId, T value, CancellationToken cancellationToken);
    private delegate Task EndpointMonitorAction<in T>(IAsyncEndpointMonitor monitor, DAsyncId flowId, T value, CancellationToken cancellationToken);

    private StartFlowState _startFlowState;

    protected sealed override Task OnStartAsync(IDAsyncFlowStartContext context, CancellationToken cancellationToken)
    {
        _startFlowState = new(context.FlowId);
        return OnStartCoreAsync(context, cancellationToken);
    }

    protected override async Task OnSuspendAsync(IDAsyncFlowSuspensionContext context, CancellationToken cancellationToken)
    {
        Reset();

        if (!_startFlowState.IsOnStart)
            return;

        DAsyncId flowId = _startFlowState.FlowId;
        Debug.Assert(flowId != default);
        Reset();

        var endpointMonitor = Services.GetRequiredService<IAsyncEndpointMonitor>();
        await endpointMonitor.SetRunningAsync(flowId, cancellationToken);

        TypedInstance<object> continuationSurrogate = _startFlowState.ContinuationSurrogate;
        TypedInstance<object>[]? continuationSurrogateArray = _startFlowState.ContinuationSurrogateArray;

        if (continuationSurrogate != default)
        {
            string callbackKey = GetContinuationKey(flowId);
            await context.Heap.SaveAsync(callbackKey, continuationSurrogate, cancellationToken);
        }
        else if (continuationSurrogateArray is not null)
        {
            string continuationKey = GetContinuationKey(flowId);
            TypedInstance<object> aggregateContinuationSurrogate = AggregateDAsyncContinuation.CreateSurrogate(continuationSurrogateArray);
            await context.Heap.SaveAsync(continuationKey, aggregateContinuationSurrogate, cancellationToken);
        }

        await SuspendOnStartAsync(flowId, cancellationToken);
    }

    protected sealed override Task OnSucceedAsync(IDAsyncFlowCompletionContext context, CancellationToken cancellationToken)
    {
        if (_startFlowState.IsOnStart)
        {
            Reset();

            // TODO: If configured, we can optionally also update the status for the status monitor
            return SucceedOnStartAsync(context, cancellationToken);
        }

        return SucceedOnResumeAsync(context, cancellationToken);
    }

    protected sealed override Task OnSucceedAsync<TResult>(IDAsyncFlowCompletionContext context, TResult result, CancellationToken cancellationToken)
    {
        if (_startFlowState.IsOnStart)
        {
            Reset();

            // TODO: If configured, we can optionally also update the status for the status monitor
            return SucceedOnStartAsync(context, result, cancellationToken);
        }

        return SucceedOnResumeAsync(context, result, cancellationToken);
    }

    protected sealed override Task OnFailAsync(IDAsyncFlowCompletionContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (_startFlowState.IsOnStart)
        {
            Reset();

            // TODO: If configured, we can optionally also update the status for the status monitor
            return FailOnStartAsync(context, exception, cancellationToken);
        }

        return FailOnResumeAsync(context, exception, cancellationToken);
    }

    protected sealed override Task OnCancelAsync(IDAsyncFlowCompletionContext context, OperationCanceledException exception, CancellationToken cancellationToken)
    {
        if (_startFlowState.IsOnStart)
        {
            Reset();

            // TODO: If configured, we can optionally also update the status for the status monitor
            return CancelOnStartAsync(context, cancellationToken);
        }

        return CancelOnResumeAsync(context, cancellationToken);
    }

    protected virtual Task OnStartCoreAsync(IDAsyncFlowStartContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual Task SuspendOnStartAsync(DAsyncId flowId, CancellationToken cancellationToken)
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
            static (monitor, flowId, value, cancellationToken) => monitor.SetSucceededAsync(flowId, cancellationToken),
            static (continuation, flowId, value, cancellationToken) => continuation.OnSucceedAsync(flowId, cancellationToken),
            cancellationToken);
    }

    protected virtual Task SucceedOnResumeAsync<TResult>(IDAsyncFlowCompletionContext context, TResult result, CancellationToken cancellationToken)
    {
        return CompleteOnResumeAsync(
            context,
            result,
            static (monitor, flowId, value, cancellationToken) => monitor.SetSucceededAsync(flowId, value, cancellationToken),
            static (continuation, flowId, value, cancellationToken) => continuation.OnSucceedAsync(flowId, value, cancellationToken),
            cancellationToken);
    }

    protected virtual Task FailOnResumeAsync(IDAsyncFlowCompletionContext context, Exception exception, CancellationToken cancellationToken)
    {
        return CompleteOnResumeAsync(
            context,
            exception,
            static (monitor, flowId, value, cancellationToken) => monitor.SetFaultedAsync(flowId, value, cancellationToken),
            static (continuation, flowId, value, cancellationToken) => continuation.OnFailAsync(flowId, value, cancellationToken),
            cancellationToken);
    }

    protected virtual Task CancelOnResumeAsync(IDAsyncFlowCompletionContext context, CancellationToken cancellationToken)
    {
        return CompleteOnResumeAsync(
            context,
            default(VoidResult),
            static (monitor, flowId, value, cancellationToken) => monitor.SetCanceledAsync(flowId, cancellationToken),
            static (continuation, flowId, value, cancellationToken) => continuation.OnCancelAsync(flowId, cancellationToken),
            cancellationToken);
    }

    protected void SetContinuation(TypedInstance<object> surrogate)
    {
        Debug.Assert(_startFlowState.IsOnStart);

        _startFlowState.ContinuationSurrogate = surrogate;
    }

    protected void SetContinuation(TypedInstance<object>[] surrogateArray)
    {
        Debug.Assert(_startFlowState.IsOnStart);

        _startFlowState.ContinuationSurrogateArray = surrogateArray;
    }

    private async Task CompleteOnResumeAsync<T>(
        IDAsyncFlowCompletionContext context,
        T value,
        EndpointMonitorAction<T> endpointMonitorAction,
        ContinuationAction<T> continuationAction,
        CancellationToken cancellationToken)
    {
        DAsyncId flowId = context.FlowId;

        var endpointMonitor = Services.GetRequiredService<IAsyncEndpointMonitor>();
        await endpointMonitorAction(endpointMonitor, flowId, value, cancellationToken);

        string continuationKey = GetContinuationKey(flowId);
        Option<TypedInstance<IDAsyncContinuationSurrogate>> loadResult = await context.Heap.LoadAsync<string, TypedInstance<IDAsyncContinuationSurrogate>>(continuationKey, cancellationToken);

        // TODO: Check before and decide what to do should it be empty
        IDAsyncContinuation continuation = loadResult.Value.Instance.Restore(Services);
        await continuationAction(continuation, flowId, value, cancellationToken);

        Reset();
    }

    private void Reset()
    {
        _startFlowState = default;
    }

    private static string GetContinuationKey(DAsyncId flowId)
    {
        // TODO: Optimize and use ReadOnlySpan
        return $"{flowId}:continuation";
    }

    public static AspNetCoreDAsyncHost Create(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return new DefaultDAsyncHost(services);
    }

    public static AspNetCoreDAsyncHost CreateAsyncEndpointHost(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        return new AsyncEndpointDAsyncHost(httpContext);
    }

    private readonly struct VoidResult;

    private struct StartFlowState(DAsyncId flowId)
    {
        public TypedInstance<object> ContinuationSurrogate;
        public TypedInstance<object>[]? ContinuationSurrogateArray;

        public bool IsOnStart { get; } = true;

        public DAsyncId FlowId { get; } = flowId;
    }

    private sealed class DefaultDAsyncHost(IServiceProvider services) : AspNetCoreDAsyncHost
    {
        protected override IServiceProvider Services => services;
    }
}
