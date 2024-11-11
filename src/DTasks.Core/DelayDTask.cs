using DTasks.Hosting;

namespace DTasks;

internal sealed class DelayDTask(TimeSpan delay) : DTask
{
    public override DTaskStatus Status => DTaskStatus.Pending;

    protected override void Run(IDAsyncFlow flow) => flow.Delay(delay);
}
