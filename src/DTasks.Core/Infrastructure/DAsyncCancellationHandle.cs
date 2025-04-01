using System.ComponentModel;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct DAsyncCancellationHandle
{
    private readonly CancellationTokenSource _localSource;

    internal DAsyncCancellationHandle(CancellationTokenSource localSource)
    {
        _localSource = localSource;
    }

    public void Cancel() => _localSource.Cancel();
}
