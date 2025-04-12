namespace DTasks.Execution;

public delegate Task SuspensionCallback(DAsyncId id, CancellationToken cancellationToken = default);

public delegate Task SuspensionCallback<in TState>(DAsyncId id, TState state, CancellationToken cancellationToken = default);
