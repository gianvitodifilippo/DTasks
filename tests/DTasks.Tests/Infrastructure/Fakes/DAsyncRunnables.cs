using DTasks.Execution;
using DTasks.Infrastructure.Features;

namespace DTasks.Infrastructure.Fakes;

internal sealed class SucceedDAsyncRunnable : IDAsyncRunnable
{
    public void Run(IDAsyncRunner runner) => runner.Succeed();
}

internal sealed class SucceedDAsyncRunnable<TResult>(TResult result) : IDAsyncRunnable
{
    public void Run(IDAsyncRunner runner) => runner.Succeed(result);
}

internal sealed class FailDAsyncRunnable(Exception exception) : IDAsyncRunnable
{
    public void Run(IDAsyncRunner runner) => runner.Fail(exception);
}

internal sealed class CancelDAsyncRunnable(OperationCanceledException exception) : IDAsyncRunnable
{
    public void Run(IDAsyncRunner runner) => runner.Cancel(exception);
}

internal sealed class YieldDAsyncRunnable : IDAsyncRunnable
{
    public void Run(IDAsyncRunner runner) => runner.Yield();
}

internal sealed class DelayDAsyncRunnable(TimeSpan delay) : IDAsyncRunnable
{
    public void Run(IDAsyncRunner runner) => runner.Delay(delay);
}

internal sealed class CallbackDAsyncRunnable(ISuspensionCallback callback) : IDAsyncRunnable
{
    public void Run(IDAsyncRunner runner) => runner.Features.GetRequiredFeature<IDAsyncSuspensionFeature>().Suspend(callback);
}

internal sealed class SucceedSuspendedDTask : DTask
{
    // This is an awaitable runnable that does not invoke its continuation right away, because its awaiter
    // is not complete, but the continuation will be invoked by its awaiter and the Succeed call
    
    public override DTaskStatus Status => DTaskStatus.Suspended;

    protected override void Run(IDAsyncRunner runner) => runner.Succeed();
}

internal sealed class SucceedSuspendedDTask<TResult>(TResult result) : DTask<TResult>
{
    public override DTaskStatus Status => DTaskStatus.Suspended;

    protected override void Run(IDAsyncRunner runner) => runner.Succeed(result);
}

internal sealed class FailSuspendedDTask(Exception exception) : DTask
{
    public override DTaskStatus Status => DTaskStatus.Suspended;

    protected override void Run(IDAsyncRunner runner) => runner.Fail(exception);
}

internal sealed class CancelSuspendedDTask(OperationCanceledException exception) : DTask
{
    public override DTaskStatus Status => DTaskStatus.Suspended;

    protected override void Run(IDAsyncRunner runner) => runner.Cancel(exception);
}