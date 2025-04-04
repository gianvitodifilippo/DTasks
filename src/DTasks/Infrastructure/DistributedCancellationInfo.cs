namespace DTasks.Infrastructure;

internal readonly record struct DistributedCancellationInfo(DAsyncCancellationHandle Handle, DateTimeOffset ExpirationTime);
