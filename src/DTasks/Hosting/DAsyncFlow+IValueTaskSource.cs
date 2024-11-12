using DTasks.Utils;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Sources;

namespace DTasks.Hosting;

internal partial class DAsyncFlow : IValueTaskSource
{
    void IValueTaskSource.GetResult(short token)
    {
        try
        {
            _ = _valueTaskSource.GetResult(token);
        }
        finally
        {
            Reset();
        }
    }

    ValueTaskSourceStatus IValueTaskSource.GetStatus(short token)
    {
        return _valueTaskSource.GetStatus(token);
    }

    void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
    {
        _valueTaskSource.OnCompleted(continuation, state, token, flags);
    }

    //private AggregateType _aggregateType;
    //private IEnumerable<IDAsyncRunnable>? _aggregateBranches;
    //private List<Exception>? _aggregateExceptions;
    //private int _whenAllBranchCount;
    //private IDictionary? _whenAllBranchResults;
    //private IDAsyncRunnable? _backgroundRunnable;
    //private object? _resultCallback;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Reset()
    {
        Assert.Null(_parent);
        Assert.Null(_suspendingAwaiterOrType);
        Assert.Null(_delay);
        Assert.Null(_callback);
        Assert.Null(_continuation);
        Assert.Null(_stateMachine);
        Assert.Null(_aggregateBranches);
        Assert.Null(_aggregateExceptions);
        Debug.Assert(_whenAllBranchCount == 0);
        Assert.Null(_whenAllBranchResults);
        Assert.Null(_backgroundRunnable);
        Assert.Null(_resultCallback);

        _state = FlowState.Pending;
        _valueTaskSource.Reset();
        _cancellationToken = default;

        _host = s_nullHost;
        _marshaler = s_nullMarshaler;
        _stateManager = s_nullStateManager;

        _parentId = default;
        _id = default;
    }
}
