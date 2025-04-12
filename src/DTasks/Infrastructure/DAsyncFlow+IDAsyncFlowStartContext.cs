using System.Diagnostics;

namespace DTasks.Infrastructure;

public sealed partial class DAsyncFlow : IDAsyncFlowStartContext
{
    private static readonly object s_resultSentinel = new();

    void IDAsyncFlowStartContext.SetResult() => SetResultOrException(s_resultSentinel);

    void IDAsyncFlowStartContext.SetException(Exception exception) => SetResultOrException(exception);

    DAsyncId IDAsyncFlowStartContext.FlowId
    {
        get
        {
            Debug.Assert(_parentId.IsFlowId);
            return _parentId;
        }
    }

    private void SetResultOrException(object resultOrException)
    {
        Debug.Assert(_state is FlowState.Starting, $"{nameof(IDAsyncResultBuilder)} should be exposed only when starting.");
        
        if (_resultOrException is not null)
            throw new InvalidOperationException("The result was already set.");
        
        _resultOrException = resultOrException;
    }
}