using DTasks.Execution;
using DTasks.Infrastructure.Features;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncSuspensionFeature
{
    void IDAsyncSuspensionFeature.Suspend(ISuspensionCallback callback)
    {
        IDAsyncStateMachine? stateMachine = Consume(ref _stateMachine);
        if (stateMachine is not null)
        {
            Assign(ref _suspensionCallback, callback);
            Assign(ref _dehydrateContinuation, static self =>
            {
                self.RunIndirection(static self => self.AwaitOnCallback());
            });
            stateMachine.Suspend();
            return;
        }
        
        Assign(ref _suspensionCallback, callback);
        RunIndirection(static self => self.AwaitOnCallback());
    }
}