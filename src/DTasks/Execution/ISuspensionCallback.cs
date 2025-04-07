namespace DTasks.Execution;

public interface ISuspensionCallback
{
    Task InvokeAsync(DAsyncId id, CancellationToken cancellationToken = default);
}

public interface ISuspensionCallback<in TState>
{
    Task InvokeAsync(DAsyncId id, TState state, CancellationToken cancellationToken = default);
}
