using System.Diagnostics;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncFlowStartContext
{
    private static readonly object s_resultSentinel = new();

    void IDAsyncFlowStartContext.SetResult()
    {
        AssertState<IDAsyncFlowStartContext>(FlowState.Starting);
        SetResultOrException(s_resultSentinel);
    }

    void IDAsyncFlowStartContext.SetException(Exception exception)
    {
        AssertState<IDAsyncFlowStartContext>(FlowState.Starting);
        SetResultOrException(exception);
    }

    DAsyncId IDAsyncFlowStartContext.FlowId
    {
        get
        {
            AssertState<IDAsyncFlowStartContext>(FlowState.Starting);
            
            return _id;
        }
    }

    private void SetResultOrException(object resultOrException)
    {
        if (_resultOrException is not null)
            throw new InvalidOperationException("The result was already set.");

        Assign(ref _resultOrException, resultOrException);
    }
}