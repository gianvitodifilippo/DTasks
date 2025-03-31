namespace DTasks.Execution;

internal readonly struct CancellationFactoryArguments(TimeSpan delay, CancellationToken cancellationToken)
{
    public TimeSpan Delay => delay;

    public CancellationToken CancellationToken => cancellationToken;
}
