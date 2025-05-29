using DTasks.Execution;
using DTasks.Infrastructure.Features;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncSuspensionFeature
{
    void IDAsyncSuspensionFeature.Suspend(ISuspensionCallback callback)
    {
        AssertState<IDAsyncSuspensionFeature>(FlowState.Running);

        Assign(ref _suspensionCallback, callback);
        AwaitRedirect(s_callbackIndirection, ErrorHandlers.Indirection.Callback);
    }
}