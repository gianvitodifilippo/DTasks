using DTasks.Execution;

namespace DTasks.Infrastructure;

internal partial class DAsyncFlow : IDAsyncCancellationManager
{
    DCancellationTokenSource IDAsyncCancellationManager.Create(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    DCancellationTokenSource IDAsyncCancellationManager.Create(TimeSpan delay, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
