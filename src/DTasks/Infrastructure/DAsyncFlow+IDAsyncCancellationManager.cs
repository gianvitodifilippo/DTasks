using System.Diagnostics;
using DTasks.Execution;
using DTasks.Infrastructure.Execution;

namespace DTasks.Infrastructure;

public sealed partial class DAsyncFlow : IDAsyncCancellationManager
{
    Task IDAsyncCancellationManager.CreateAsync(DCancellationTokenSource source, DAsyncCancellationHandle handle, CancellationToken cancellationToken)
    {
        _ = Register(source, handle);
        return Task.CompletedTask;
    }

    Task IDAsyncCancellationManager.CreateAsync(DCancellationTokenSource source, DAsyncCancellationHandle handle, TimeSpan delay, CancellationToken cancellationToken)
    {
        DateTimeOffset expirationTime = DateTimeOffset.Now + delay;
        DCancellationId id = Register(source, handle);
        return _host.CancellationProvider.CancelAsync(id, expirationTime, cancellationToken);
    }

    Task IDAsyncCancellationManager.CancelAsync(DCancellationTokenSource source, CancellationToken cancellationToken)
    {
        CancellationInfo info = _cancellationInfos[source];
        return _host.CancellationProvider.CancelAsync(info.Id, cancellationToken);
    }

    Task IDAsyncCancellationManager.CancelAfterAsync(DCancellationTokenSource source, TimeSpan delay, CancellationToken cancellationToken)
    {
        DateTimeOffset expirationTime = DateTimeOffset.Now + delay;
        CancellationInfo info = _cancellationInfos[source];
        return _host.CancellationProvider.CancelAsync(info.Id, expirationTime, cancellationToken);
    }

    private DCancellationId Register(DCancellationTokenSource source, DAsyncCancellationHandle handle)
    {
        DCancellationId id;
        do
        {
            id = DCancellationId.New();
        }
        while (!_cancellations.TryAdd(id, source));

        CancellationInfo info = new(id, handle);
        bool added = _cancellationInfos.TryAdd(source, info);
        Debug.Assert(added, "Attempted to register a cancellation source multiple times.");

        return id;
    }

    private readonly record struct CancellationInfo(DCancellationId Id, DAsyncCancellationHandle Handle);
}
