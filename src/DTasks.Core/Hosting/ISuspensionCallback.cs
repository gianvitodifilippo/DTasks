namespace DTasks.Hosting;

public interface ISuspensionCallback
{
    Task OnSuspendedAsync<TFlowId>(TFlowId flowId, CancellationToken cancellationToken = default)
        where TFlowId : notnull;
}

public interface ISuspensionCallback<TState>
{
    Task OnSuspendedAsync<TFlowId>(TFlowId flowId, TState state, CancellationToken cancellationToken = default)
        where TFlowId : notnull;
}
