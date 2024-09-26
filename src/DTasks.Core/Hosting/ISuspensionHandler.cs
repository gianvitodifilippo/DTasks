namespace DTasks.Hosting;

public interface ISuspensionHandler
{
    Task OnDelayAsync(TimeSpan delay, CancellationToken cancellationToken = default);

    Task OnYieldAsync(CancellationToken cancellationToken = default);

    Task OnSuspendedAsync(ISuspensionCallback callback, CancellationToken cancellationToken = default);
}
