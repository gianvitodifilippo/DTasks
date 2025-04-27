using DTasks.Execution;
using DTasks.Infrastructure.Execution;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncCancellationHandler
{
    void IDAsyncCancellationHandler.Cancel(DCancellationId id)
    {
        if (!_cancellations.TryGetValue(id, out DCancellationTokenSource? source))
            return;

        _cancellationInfos[source].Handle.Cancel();
    }
}
