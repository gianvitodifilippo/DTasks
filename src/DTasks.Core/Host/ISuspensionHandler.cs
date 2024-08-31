namespace DTasks.Host;

public interface ISuspensionHandler
{
    void SaveStateMachine<TStateMachine>(ref TStateMachine stateMachine, ISuspensionInfo suspensionInfo)
        where TStateMachine : notnull;

    Task OnDelayAsync(TimeSpan delay, CancellationToken cancellationToken);

    Task OnYieldAsync(CancellationToken cancellationToken);

    Task OnSuspendedAsync(ISuspensionCallback callback, CancellationToken cancellationToken);
}
