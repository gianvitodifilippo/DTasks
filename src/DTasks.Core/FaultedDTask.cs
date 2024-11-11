using DTasks.Hosting;

namespace DTasks;

internal sealed class FaultedDTask(Exception exception) : DTask
{
    public override DTaskStatus Status => DTaskStatus.Faulted;

    protected override Exception ExceptionCore => exception;

    protected override void Run(IDAsyncFlow flow) => flow.Resume(exception);
}

internal sealed class FaultedDTask<TResult>(Exception exception) : DTask<TResult>
{
    public override DTaskStatus Status => DTaskStatus.Faulted;

    protected override Exception ExceptionCore => exception;

    protected override void Run(IDAsyncFlow flow) => flow.Resume(exception);
}
