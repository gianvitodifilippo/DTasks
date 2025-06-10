using System.Diagnostics;
using System.Threading.Tasks.Sources;
using DTasks.Marshaling;
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

    private void HandleException(Exception exception)
    {
        if (exception is not MarshalingException)
        {
            ErrorMessageProvider messageProvider = Consume(ref _errorMessageProvider) ?? ErrorMessages.Default;
            string message = messageProvider(this);

            exception = new DAsyncInfrastructureException(message, exception);
        }
        
        _hasError = true;
        _valueTaskSource.SetException(exception);
    }

    private void Reset()
    {
        if (_hasError)
        {
            _errorMessageProvider = null;
            _resultOrException = null;
            _runnable = null;
            _stateMachine = null;
            _suspendingAwaiterOrType = null;
            _childStateMachine = null;
            _delay = null;
            _suspensionCallback = null;
            _dehydrateContinuation = null;
            _resultBuilder = null;
            _handleResultHandler = null;
            _handleId = default;
            _nodeProperties?.Clear();
        }
        else
        {
            Assert.Null(_errorMessageProvider);
            Assert.Null(_resultOrException);
            Assert.Null(_runnable);
            Assert.Null(_stateMachine);
            Assert.Null(_suspendingAwaiterOrType);
            Assert.Null(_childStateMachine);
            Assert.Null(_delay);
            Assert.Null(_suspensionCallback);
            Assert.Null(_dehydrateContinuation);
            Assert.Null(_resultBuilder);
            Assert.Null(_handleResultHandler);
            Assert.Default(_handleId);
            Debug.Assert(_nodeProperties is null or { Count: 0 });
        }

        _heap = null;
        _stack = null;
        _surrogator = null;
        _cancellationProvider = null;
        _suspensionHandler = null;

        _state = FlowState.Pending;
        _cancellationToken = CancellationToken.None;
        _flowComponentProvider.EndScope();
        _valueTaskSource.Reset();
        _parentId = default;
        _id = default;
        
        _handleIds?.Clear();
        _completedTasks?.Clear();
        
        // _cancellationProvider?.UnregisterHandler(this);
        
        // TODO: Invoke this before Reset
        // _host.OnFinalize(this);

        // _parent = null;

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
