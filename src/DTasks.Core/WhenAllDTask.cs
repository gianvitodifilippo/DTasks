namespace DTasks;

internal class WhenAllDTask<TResult> : DTask<TResult>
{
    internal override TResult Result => throw new NotImplementedException();

    internal override Task<bool> UnderlyingTask => throw new NotImplementedException();

    internal override DTaskStatus Status => throw new NotImplementedException();

    internal override Task SuspendAsync<THandler>(ref THandler handler, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
