namespace DTasks;

internal class WhenAllDTask(IEnumerable<DTask> tasks) : DTask
{
    internal override DTaskStatus Status => DTaskStatus.Suspended; // TODO: Support local completion

    internal override Task<bool> UnderlyingTask => Task.FromResult(false);

    internal override Task SuspendAsync<THandler>(ref THandler handler, CancellationToken cancellationToken)
    {
        return handler.OnWhenAllAsync(tasks, cancellationToken);
    }
}
