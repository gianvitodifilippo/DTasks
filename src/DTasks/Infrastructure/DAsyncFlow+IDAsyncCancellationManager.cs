using System.Diagnostics;
using DTasks.Execution;

namespace DTasks.Infrastructure;

internal partial class DAsyncFlow : IDAsyncCancellationManager
{
    Task IDAsyncCancellationManager.CreateAsync(DCancellationTokenSource source, DAsyncCancellationHandle handle, CancellationToken cancellationToken)
    {
        DistributedCancellationInfo cancellationInfo = new(handle, DateTimeOffset.MaxValue);
        bool added = _cancellations.TryAdd(source, cancellationInfo);
        Debug.Assert(added, "Attempted to register a cancellation source multiple times.");

        return Task.CompletedTask;
    }

    async Task IDAsyncCancellationManager.CreateAsync(DCancellationTokenSource source, DAsyncCancellationHandle handle, TimeSpan delay, CancellationToken cancellationToken)
    {
        DistributedCancellationInfo cancellationInfo = new(handle, DateTimeOffset.UtcNow + delay);
        bool added = _cancellations.TryAdd(source, cancellationInfo);
        Debug.Assert(added, "Attempted to register a cancellation source multiple times.");

        // TODO: Register with external provider
        throw new NotImplementedException();
    }

    Task IDAsyncCancellationManager.CancelAfterAsync(DCancellationTokenSource source, TimeSpan delay, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    Task IDAsyncCancellationManager.CancelAsync(DCancellationTokenSource source, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    bool IDAsyncCancellationManager.IsCancellationRequested(DCancellationTokenSource source)
    {
        DistributedCancellationInfo cancellationInfo = _cancellations[source];
        return cancellationInfo.ExpirationTime <= DateTimeOffset.UtcNow;
    }
}
