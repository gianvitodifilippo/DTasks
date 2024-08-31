namespace DTasks;

internal sealed class YieldDTask : DTask
{
    public static readonly YieldDTask Instance = new();

    private YieldDTask() { }

    public override DTaskStatus Status => DTaskStatus.Suspended;

    private protected override Task<bool> UnderlyingTask => Task.FromResult(false);

    internal override Task OnSuspendedAsync<THandler>(ref THandler handler, CancellationToken cancellationToken)
    {
        return handler.OnYieldAsync(cancellationToken);
    }
}
