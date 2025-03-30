namespace DTasks.Infrastructure;

public interface ISuspensionCallback
{
    Task InvokeAsync(DAsyncId id, CancellationToken cancellationToken = default);
}

public interface ISuspensionCallback<TState>
{
    Task InvokeAsync(DAsyncId id, TState state, CancellationToken cancellationToken = default);
}
