namespace DTasks;

internal sealed class YieldDTask : DTask
{
    public static readonly YieldDTask Instance = new();

    private YieldDTask() { }

    internal override DTaskStatus Status => DTaskStatus.Suspended;

    internal override Task<bool> UnderlyingTask => Task.FromResult(false);

    internal override Task SuspendAsync<THandler>(ref THandler handler, CancellationToken cancellationToken)
    {
        return handler.OnYieldAsync(cancellationToken);
    }
}
