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
        Assign(ref _suspensionCallback, callback);
        
        if (_stateMachine is not null)
        {
            Suspend(self => self.RunCallbackIndirection());
            return;
        }
        
        RunCallbackIndirection();
    }
}