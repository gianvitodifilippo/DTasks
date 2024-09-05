namespace DTasks;

internal sealed class DelayDTask(TimeSpan delay) : DTask
{
    internal override DTaskStatus Status => DTaskStatus.Suspended;

    internal override Task<bool> UnderlyingTask => Task.FromResult(false);

    internal override Task SuspendAsync<THandler>(ref THandler handler, CancellationToken cancellationToken)
    {
        return handler.OnDelayAsync(delay, cancellationToken);
    }
}
