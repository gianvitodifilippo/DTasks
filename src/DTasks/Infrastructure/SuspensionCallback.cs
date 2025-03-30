namespace DTasks.Infrastructure;

public delegate Task SuspensionCallback(DAsyncId id, CancellationToken cancellationToken = default);

public delegate Task SuspensionCallback<TState>(DAsyncId id, TState state, CancellationToken cancellationToken = default);
