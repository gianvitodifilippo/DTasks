﻿using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Sources;
using DTasks.Utils;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IValueTaskSource
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Reset()
    {
        Assert.Null(_suspendingAwaiterOrType);
        Assert.Null(_delay);
        Assert.Null(_callback);
        Assert.Null(_continuation);
        Debug.Assert(_aggregateType is AggregateType.None);
        Assert.Null(_aggregateBranches);
        Assert.Null(_aggregateExceptions);
        Debug.Assert(_branchCount == 0);
        Assert.Null(_whenAllBranchResults);
        Assert.Null(_aggregateRunnable);
        Assert.Null(_resultBuilder);
        Assert.Null(_resultOrException);
        Debug.Assert(_branchIndex == -1);

        _cancellationProvider?.UnregisterHandler(this);
        _host.OnFinalize(this);

        _state = FlowState.Pending;
        _runnable = null;
        _valueTaskSource.Reset();
        _cancellationToken = CancellationToken.None;
        _flowComponentProvider.EndScope();

        _parentId = default;
        _id = default;
        _stateMachine = null;
        _parent = null;

        _heap = null;
        _stack = null;
        _surrogator = null;
        _cancellationProvider = null;
        _suspensionHandler = null;

        _surrogates.Clear();
        _tasks.Clear();
        _cancellationInfos.Clear();
        _cancellations.Clear();

        if (_clearFlowProperties)
        {
            _flowProperties?.Clear();
        }

#if DEBUG
        _stackTrace = null;
#endif
    }
}
