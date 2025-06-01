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
        ErrorMessageProvider messageProvider = Consume(ref _errorMessageProvider) ?? ErrorMessages.Default;
        string message = messageProvider(this);
        
        DAsyncInfrastructureException exception = new(message, innerException);
        
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
        
        // _cancellationProvider?.UnregisterHandler(this);
        _host.OnFinalize(this);

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
