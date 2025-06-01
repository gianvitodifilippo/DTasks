using DTasks.Execution;
using DTasks.Infrastructure.Features;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncSuspensionFeature
{
    private void RunSuspendIndirection()
    {
        RunIndirection(static self => self.AwaitCallbackInvoke());
    }
    
    void IDAsyncSuspensionFeature.Suspend(ISuspensionCallback callback)
    {
        IDAsyncStateMachine? stateMachine = Consume(ref _stateMachine);
        Assign(ref _suspensionCallback, callback);
        
        if (stateMachine is null)
        {
            RunSuspendIndirection();
            return;
        }
        
        Assign(ref _dehydrateContinuation, static self => self.RunSuspendIndirection());
        stateMachine.Suspend();
    }
}