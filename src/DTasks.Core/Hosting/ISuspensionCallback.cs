namespace DTasks.Hosting;

public interface ISuspensionCallback
{
    Task OnSuspendedAsync<TFlowId>(TFlowId flowId, CancellationToken cancellationToken)
        where TFlowId : notnull;
}
