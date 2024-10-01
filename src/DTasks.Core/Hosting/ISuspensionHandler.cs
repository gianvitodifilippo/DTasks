namespace DTasks.Hosting;

public interface ISuspensionHandler
{
    Task OnCallbackAsync<TCallback>(TCallback callback, CancellationToken cancellationToken = default)
        where TCallback : ISuspensionCallback;

    Task OnCallbackAsync<TState, TCallback>(TState state, TCallback callback, CancellationToken cancellationToken = default)
        where TCallback: ISuspensionCallback<TState>;

    Task OnDelayAsync(TimeSpan delay, CancellationToken cancellationToken = default);

    Task OnYieldAsync(CancellationToken cancellationToken = default);

    Task OnWhenAllAsync(IEnumerable<DTask> tasks, CancellationToken cancellationToken = default);

    Task OnWhenAllAsync<TResult>(IEnumerable<DTask<TResult>> tasks, CancellationToken cancellationToken = default);
}
