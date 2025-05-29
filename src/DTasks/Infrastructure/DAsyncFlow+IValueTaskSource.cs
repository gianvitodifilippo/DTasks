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

    private void SetInfrastructureException(Exception innerException)
    {
        InfrastructureErrorHandler errorHandler = Consume(ref _errorHandler) ?? ErrorHandlers.Default;
        string message = errorHandler(this);
        
        DAsyncInfrastructureException exception = new(message, innerException);
        _valueTaskSource.SetException(exception);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Reset()
    {
        Assert.Null(_errorHandler);
        Assert.Null(_indirectionErrorHandler);
        Assert.Null(_continuation);
        Assert.Null(_resultOrException);
        Assert.Null(_runnable);
        Assert.Null(_stateMachine);
        Assert.Null(_suspendingAwaiterOrType);
        Assert.Null(_delay);
        Assert.Null(_suspensionCallback);
        
        // _cancellationProvider?.UnregisterHandler(this);
        // _host.OnFinalize(this);

        _state = FlowState.Pending;
        _cancellationToken = CancellationToken.None;
        _flowComponentProvider.EndScope();
        _valueTaskSource.Reset();
        _parentId = default;
        _id = default;

        // _stateMachine = null;
        // _parent = null;

        _heap = null;
        _stack = null;
        _surrogator = null;
        _cancellationProvider = null;
        _suspensionHandler = null;

        // _surrogates.Clear();
        // _tasks.Clear();
        // _cancellationInfos.Clear();
        // _cancellations.Clear();
        //
        // if (_clearFlowProperties)
        // {
        //     _flowProperties?.Clear();
        // }

#if DEBUG
        _stackTrace = null;
#endif
    }
}
