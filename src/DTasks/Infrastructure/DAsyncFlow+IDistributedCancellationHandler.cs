using DTasks.Execution;

namespace DTasks.Infrastructure;

internal partial class DAsyncFlow : IDAsyncCancellationHandler
{
    void IDAsyncCancellationHandler.Cancel(DCancellationId id)
    {
        if (!_cancellations.TryGetValue(id, out DCancellationTokenSource? source))
            return;

        _cancellationInfos[source].Handle.Cancel();
    }
}
