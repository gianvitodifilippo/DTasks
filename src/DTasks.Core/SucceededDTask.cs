using DTasks.Infrastructure;

namespace DTasks;

internal sealed class SucceededDTask : DTask
{
    public override DTaskStatus Status => DTaskStatus.Succeeded;

    protected override void Run(IDAsyncRunner runner) => runner.Succeed();
}

internal sealed class SucceededDTask<TResult>(TResult result) : DTask<TResult>
{
    public override DTaskStatus Status => DTaskStatus.Succeeded;

    protected override TResult ResultCore => result;

    protected override void Run(IDAsyncRunner runner) => runner.Succeed(result);
}
