using DTasks.Execution;
using DTasks.Infrastructure;

namespace DTasks;

internal sealed class DelegateCallbackDTask<TResult>(SuspensionCallback callback) : DTask<TResult>, ISuspensionCallback
{
    public override DTaskStatus Status => DTaskStatus.Suspended;

    public Task InvokeAsync(DAsyncId id, CancellationToken cancellationToken = default)
    {
        return callback(id, cancellationToken);
    }

    protected override void Run(IDAsyncRunner runner)
    {
        if (runner is not IDAsyncRunnerInternal runnerInternal)
            throw new ArgumentException("The provided runner does not support callbacks.", nameof(runner));

        runnerInternal.Callback(this);
    }
}

internal sealed class DelegateCallbackDTask<TResult, TState>(TState state, SuspensionCallback<TState> callback) : DTask<TResult>, ISuspensionCallback
{
    public override DTaskStatus Status => DTaskStatus.Suspended;

    public Task InvokeAsync(DAsyncId id, CancellationToken cancellationToken = default)
    {
        return callback(id, state, cancellationToken);
    }

    protected override void Run(IDAsyncRunner runner)
    {
        if (runner is not IDAsyncRunnerInternal runnerInternal)
            throw new ArgumentException("The provided runner does not support callbacks.", nameof(runner));

        runnerInternal.Callback(this);
    }
}
