namespace DTasks;

internal sealed class CompletedDTask<TResult>(TResult result) : DTask<TResult>
{
    internal override DTaskStatus Status => DTaskStatus.RanToCompletion;

    internal override TResult Result => result;

    internal override Task<bool> UnderlyingTask => Task.FromResult(true);

    internal override Task SuspendAsync<THandler>(ref THandler handler, CancellationToken cancellationToken)
    {
        InvalidStatus(expectedStatus: DTaskStatus.Suspended);
        return Task.CompletedTask;
    }
}
