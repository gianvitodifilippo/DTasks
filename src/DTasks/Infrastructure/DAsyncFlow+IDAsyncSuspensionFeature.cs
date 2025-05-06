using DTasks.Execution;
using DTasks.Infrastructure.Features;
using DTasks.Utils;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncSuspensionFeature
{
    void IDAsyncSuspensionFeature.Suspend(ISuspensionCallback callback)
    {
        ThrowHelper.ThrowIfNull(callback);
        Assert.Null(_continuation);
        Assert.Null(_callback);

        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);
        _callback = callback;

        if (currentStateMachine is null)
        {
            RunIndirection(Continuations.Callback);
        }
        else
        {
            _continuation = Continuations.CallbackIndirection;
            currentStateMachine.Suspend();
        }
    }
}