using DTasks.Execution;
using DTasks.Infrastructure.Features;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncSuspensionFeature
{
    void IDAsyncSuspensionFeature.Suspend(ISuspensionCallback callback)
    {
        Assign(ref _suspensionCallback, callback);
        RunIndirection(static flow => flow.AwaitOnCallback());
    }
}