using System.Diagnostics;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncFlowCompletionContext
{
    DAsyncId IDAsyncFlowCompletionContext.FlowId
    {
        get
        {
            AssertState<IDAsyncFlowCompletionContext>(FlowState.Returning);
            
            return _id;
        }
    }
}