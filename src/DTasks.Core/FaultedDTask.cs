using DTasks.Hosting;

namespace DTasks;

internal sealed class FaultedDTask(Exception exception) : DTask
{
    public override DTaskStatus Status => DTaskStatus.Faulted;

    protected override Exception ExceptionCore => exception;

    protected override void Run(IDAsyncFlow flow) => flow.Fail(exception);
}

internal sealed class FaultedDTask<TResult>(Exception exception) : DTask<TResult>
{
    public override DTaskStatus Status => DTaskStatus.Faulted;

    protected override Exception ExceptionCore => exception;

    protected override void Run(IDAsyncFlow flow) => flow.Fail(exception);
}
