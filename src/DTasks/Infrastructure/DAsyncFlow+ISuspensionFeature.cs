using DTasks.Execution;
using DTasks.Infrastructure.Execution;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : ISuspensionFeature
{
    private void RunCallbackIndirection()
    {
        RunIndirection(static self => self.AwaitCallbackInvoke());
    }
    
    void ISuspensionFeature.Suspend(ISuspensionCallback callback)
    {
        IDAsyncStateMachine? stateMachine = Consume(ref _stateMachine);
        Assign(ref _suspensionCallback, callback);
        
        if (stateMachine is null)
        {
            RunCallbackIndirection();
            return;
        }
        
        Assign(ref _dehydrateContinuation, static self => self.RunCallbackIndirection());
        stateMachine.Suspend();
    }
}