namespace DTasks.Hosting;

public interface ISuspensionHandler
{
    Task OnDelayAsync(TimeSpan delay, CancellationToken cancellationToken);

    Task OnYieldAsync(CancellationToken cancellationToken);

    Task OnSuspendedAsync(ISuspensionCallback callback, CancellationToken cancellationToken);
}
