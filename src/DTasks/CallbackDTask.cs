using DTasks.Infrastructure;

namespace DTasks;

internal sealed class CallbackDTask<TResult, TCallback>(TCallback callback) : DTask<TResult>, ISuspensionCallback
    where TCallback : ISuspensionCallback
{
    public override DTaskStatus Status => DTaskStatus.Suspended;

    public Task InvokeAsync(DAsyncId id, CancellationToken cancellationToken = default)
    {
        return callback.InvokeAsync(id, cancellationToken);
    }

    protected override void Run(IDAsyncRunner runner)
    {
        if (runner is not IDAsyncRunnerInternal runnerInternal)
            throw new ArgumentException("The provided runner does not support callbacks.", nameof(runner));

        runnerInternal.Callback(this);
    }
}

internal sealed class CallbackDTask<TResult, TState, TCallback>(TState state, TCallback callback) : DTask<TResult>, ISuspensionCallback
    where TCallback : ISuspensionCallback<TState>
{
    public override DTaskStatus Status => DTaskStatus.Suspended;

    public Task InvokeAsync(DAsyncId id, CancellationToken cancellationToken = default)
    {
        return callback.InvokeAsync(id, state, cancellationToken);
    }

    protected override void Run(IDAsyncRunner runner)
    {
        if (runner is not IDAsyncRunnerInternal runnerInternal)
            throw new ArgumentException("The provided runner does not support callbacks.", nameof(runner));

        runnerInternal.Callback(this);
    }
}
