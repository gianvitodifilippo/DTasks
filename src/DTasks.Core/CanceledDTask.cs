using DTasks.Infrastructure;

namespace DTasks;

internal class CanceledDTask(CancellationToken cancellationToken) : DTask
{
    private OperationCanceledException? _exception;

    private new OperationCanceledException Exception => _exception ?? new DTaskCanceledException(this);

    public override DTaskStatus Status => DTaskStatus.Canceled;

    protected override Exception ExceptionCore => Exception;

    protected override CancellationToken CancellationTokenCore => cancellationToken;

    protected override void Run(IDAsyncRunner runner) => runner.Cancel(Exception);
}