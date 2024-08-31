namespace DTasks;

internal sealed class DelayDTask(TimeSpan delay) : DTask
{
    public override DTaskStatus Status => DTaskStatus.Suspended;

    private protected override Task<bool> UnderlyingTask => Task.FromResult(false);

    internal override Task OnSuspendedAsync<THandler>(ref THandler handler, CancellationToken cancellationToken)
    {
        return handler.OnDelayAsync(delay, cancellationToken);
    }
}
