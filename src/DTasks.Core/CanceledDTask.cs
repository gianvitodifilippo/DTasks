using DTasks.Infrastructure;

namespace DTasks;

internal class CanceledDTask(CancellationToken cancellationToken) : DTask
{
    public override DTaskStatus Status => DTaskStatus.Canceled;

    protected override Exception ExceptionCore => new DTaskCanceledException(this);

    protected override CancellationToken CancellationTokenCore => cancellationToken;

    protected override void Run(IDAsyncRunner runner) => runner.Cancel(cancellationToken);
}