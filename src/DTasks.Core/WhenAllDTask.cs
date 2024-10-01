namespace DTasks;

internal sealed class WhenAllDTask(IEnumerable<DTask> tasks) : DTask
{
    internal override DTaskStatus Status => DTaskStatus.Suspended; // TODO: Support local completion?

    internal override Task<bool> UnderlyingTask => Task.FromResult(false);

    internal override Task SuspendAsync<THandler>(ref THandler handler, CancellationToken cancellationToken)
    {
        return handler.OnWhenAllAsync(tasks, cancellationToken);
    }
}

internal sealed class WhenAllDTask<TResult>(IEnumerable<DTask<TResult>> tasks) : DTask<TResult[]>
{
    internal override TResult[] Result
    {
        get
        {
            InvalidStatus(expectedStatus: DTaskStatus.RanToCompletion);
            return default!;
        }
    }

    internal override DTaskStatus Status => DTaskStatus.Suspended;

    internal override Task<bool> UnderlyingTask => Task.FromResult(false);

    internal override Task SuspendAsync<THandler>(ref THandler handler, CancellationToken cancellationToken)
    {
        return handler.OnWhenAllAsync(tasks, cancellationToken);
    }
}
