using System.Diagnostics;

namespace DTasks.Infrastructure;

public sealed partial class DAsyncFlow : IDAsyncFlowCompletionContext
{
    DAsyncId IDAsyncFlowCompletionContext.FlowId
    {
        get
        {
            Debug.Assert(_id.IsFlowId);
            return _id;
        }
    }
}