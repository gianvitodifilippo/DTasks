namespace DTasks.Infrastructure;

public readonly record struct DistributedCancellationInfo(DAsyncCancellationHandle Handle, DateTimeOffset ExpirationTime);
