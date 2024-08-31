namespace DTasks;

internal sealed class CompletedDTask<TResult>(TResult result) : DTask<TResult>
{
    public override DTaskStatus Status => DTaskStatus.RanToCompletion;

    internal override TResult Result => result;

    private protected override Task<bool> UnderlyingTask => Task.FromResult(true);

    internal override Task OnSuspendedAsync<THandler>(ref THandler handler, CancellationToken cancellationToken)
    {
        InvalidStatus(DTaskStatus.Suspended);
        return Task.CompletedTask;
    }
}
