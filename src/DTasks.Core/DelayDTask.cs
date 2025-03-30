using DTasks.Infrastructure;

namespace DTasks;

internal sealed class DelayDTask(TimeSpan delay) : DTask
{
    public override DTaskStatus Status => DTaskStatus.Pending;

    protected override void Run(IDAsyncRunner runner) => runner.Delay(delay);
}
