using System.Diagnostics;

namespace DTasks.Hosting;

public abstract class DTaskHost<TContext>
{
    public Task SuspendAsync(TContext context, IDTaskScope scope, DTask.DAwaiter awaiter, CancellationToken cancellationToken = default)
    {
        return SuspendCoreAsync(context, scope, awaiter, cancellationToken);
    }

    public Task SuspendAsync<TResult>(TContext context, IDTaskScope scope, DTask<TResult>.DAwaiter awaiter, CancellationToken cancellationToken = default)
    {
        return SuspendCoreAsync(context, scope, awaiter, cancellationToken);
    }

    public Task ResumeAsync(FlowId id, IDTaskScope scope, CancellationToken cancellationToken = default)
    {
        return ResumeCoreAsync(id, scope, cancellationToken);
    }

    public Task ResumeAsync<TResult>(FlowId id, IDTaskScope scope, TResult result, CancellationToken cancellationToken = default)
    {
        return ResumeCoreAsync(id, scope, result, cancellationToken);
    }

    protected virtual Task OnCallbackAsync<TCallback>(FlowId id, IDTaskScope scope, TCallback callback, CancellationToken cancellationToken)
        where TCallback : ISuspensionCallback
    {
        return callback.OnSuspendedAsync(id, cancellationToken);
    }

    protected virtual Task OnCallbackAsync<TState, TCallback>(FlowId id, IDTaskScope scope, TState state, TCallback callback, CancellationToken cancellationToken)
        where TCallback : ISuspensionCallback<TState>
    {
        return callback.OnSuspendedAsync(id, state, cancellationToken);
    }

    protected virtual Task OnDelayAsync(FlowId id, IDTaskScope scope, TimeSpan delay, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("The current DTask host does not support 'DTask.Delay'.");
    }

    protected virtual Task OnYieldAsync(FlowId id, IDTaskScope scope, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("The current DTask host does not support 'DTask.Yield'.");
    }

    protected virtual Task OnWhenAllAsync(FlowId id, IDTaskScope scope, IEnumerable<DTask> tasks, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("The current DTask host does not support 'DTask.WhenAll'.");
    }

    protected abstract Task OnCompletedAsync(FlowId id, TContext context, CancellationToken cancellationToken);

    protected abstract Task OnCompletedAsync<TResult>(FlowId id, TContext context, TResult result, CancellationToken cancellationToken);

    protected abstract Task SuspendCoreAsync(TContext context, IDTaskScope scope, DTask.DAwaiter awaiter, CancellationToken cancellationToken);

    protected abstract Task SuspendCoreAsync<TResult>(TContext context, IDTaskScope scope, DTask<TResult>.DAwaiter awaiter, CancellationToken cancellationToken);

    protected abstract Task ResumeCoreAsync(FlowId id, IDTaskScope scope, CancellationToken cancellationToken);

    protected abstract Task ResumeCoreAsync<TResult>(FlowId id, IDTaskScope scope, TResult result, CancellationToken cancellationToken);

    protected Task OnHostedCompletedAsync(FlowId id, TContext context, DTask.DAwaiter awaiter, CancellationToken cancellationToken)
    {
        Debug.Assert(id.Kind is FlowKind.Hosted, "Expected a hosted flow id.");

        HostedFlowCompletionHandler handler = new(id, context, this);
        return awaiter.CompleteAsync(ref handler, cancellationToken);
    }

    // TODO: Maybe we should call it differently to make clear that's a flow child of the one being resumed
    protected Task OnAggregateCompletedAsync(FlowId id, IDTaskScope scope, DTask.DAwaiter awaiter, CancellationToken cancellationToken)
    {
        Debug.Assert(id.Kind.IsAggregate(), "Expected an aggregate flow id.");

        AggregateFlowCompletionHandler handler = new(id, scope, this);
        return awaiter.CompleteAsync(ref handler, cancellationToken);
    }

    protected Task OnSuspendedAsync(FlowId id, IDTaskScope scope, DTask.DAwaiter awaiter, CancellationToken cancellationToken)
    {
        SuspensionHandler handler = new(id, scope, this);
        return awaiter.SuspendAsync(ref handler, cancellationToken);
    }

    private readonly struct HostedFlowCompletionHandler(FlowId id, TContext context, DTaskHost<TContext> host) : ICompletionHandler
    {
        Task ICompletionHandler.OnCompletedAsync(CancellationToken cancellationToken)
        {
            return host.OnCompletedAsync(id, context, cancellationToken);
        }

        Task ICompletionHandler.OnCompletedAsync<TResult>(TResult result, CancellationToken cancellationToken)
        {
            return host.OnCompletedAsync(id, context, result, cancellationToken);
        }
    }

    // TODO: Maybe we should call it differently to make clear that's a flow child of the one being resumed
    private readonly struct AggregateFlowCompletionHandler(FlowId id, IDTaskScope scope, DTaskHost<TContext> host) : ICompletionHandler
    {
        Task ICompletionHandler.OnCompletedAsync(CancellationToken cancellationToken)
        {
            return host.ResumeAsync(id, scope, cancellationToken);
        }

        Task ICompletionHandler.OnCompletedAsync<TResult>(TResult result, CancellationToken cancellationToken)
        {
            return host.ResumeAsync(id, scope, result, cancellationToken);
        }
    }

    private readonly struct SuspensionHandler(FlowId id, IDTaskScope scope, DTaskHost<TContext> host) : ISuspensionHandler
    {
        Task ISuspensionHandler.OnCallbackAsync<TCallback>(TCallback callback, CancellationToken cancellationToken)
        {
            return host.OnCallbackAsync(id, scope, callback, cancellationToken);
        }

        Task ISuspensionHandler.OnCallbackAsync<TState, TCallback>(TState state, TCallback callback, CancellationToken cancellationToken)
        {
            return host.OnCallbackAsync(id, scope, state, callback, cancellationToken);
        }

        Task ISuspensionHandler.OnDelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            return host.OnDelayAsync(id, scope, delay, cancellationToken);
        }

        Task ISuspensionHandler.OnYieldAsync(CancellationToken cancellationToken)
        {
            return host.OnYieldAsync(id, scope, cancellationToken);
        }

        Task ISuspensionHandler.OnWhenAllAsync(IEnumerable<DTask> tasks, CancellationToken cancellationToken)
        {
            return host.OnWhenAllAsync(id, scope, tasks, cancellationToken);
        }
    }
}
