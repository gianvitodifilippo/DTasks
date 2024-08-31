namespace DTasks.Host;

public delegate Task SuspensionCallback(object flowId, CancellationToken cancellationToken);
