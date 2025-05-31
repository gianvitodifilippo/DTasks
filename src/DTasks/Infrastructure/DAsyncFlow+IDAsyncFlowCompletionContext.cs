using System.Diagnostics;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncFlowCompletionContext
{
    DAsyncId IDAsyncFlowCompletionContext.FlowId
    {
        get
        {
            AssertState<IDAsyncFlowCompletionContext>(FlowState.Terminating);
            
            return _id;
        }
    }
}