namespace DTasks.Hosting;

public delegate Task SuspensionCallback(object flowId, CancellationToken cancellationToken = default);

public delegate Task SuspensionCallback<TState>(object flowId, TState state, CancellationToken cancellationToken = default);
