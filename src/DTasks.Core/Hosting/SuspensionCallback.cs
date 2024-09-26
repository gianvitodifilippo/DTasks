namespace DTasks.Hosting;

public delegate Task SuspensionCallback(object flowId, CancellationToken cancellationToken = default);
