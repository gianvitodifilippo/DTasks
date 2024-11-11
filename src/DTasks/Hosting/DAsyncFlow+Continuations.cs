using System.Diagnostics;

namespace DTasks.Hosting;

internal partial class DAsyncFlow
{
    private static readonly Continuation s_startContinuation = flow => flow.Start();
    private static readonly Continuation s_yieldContinuation = flow => flow.Yield();
    private static readonly Continuation s_delayContinuation = flow => flow.Delay();
    private static readonly Continuation s_callbackContinuation = flow => flow.Callback();
    private static readonly Continuation s_yieldIndirectionContinuation = flow => flow.RunIndirection(s_yieldContinuation);
    private static readonly Continuation s_delayIndirectionContinuation = flow => flow.RunIndirection(s_delayContinuation);
    private static readonly Continuation s_callbackIndirectionContinuation = flow => flow.RunIndirection(s_callbackContinuation);

    private void Start()
    {
        Debug.Assert(_stateMachine is not null);

        IDAsyncStateMachine stateMachine = Consume(ref _stateMachine);
        stateMachine.Start(this);
        stateMachine.MoveNext();
    }

    private void Yield()
    {
        _suspendingAwaiterOrType = null;
        _state = FlowState.Returning;

        try
        {
            Await(_host.YieldAsync(_id, _cancellationToken));
        }
        catch (Exception ex)
        {
            _valueTaskSource.SetException(ex);
        }
    }

    private void Delay()
    {
        _suspendingAwaiterOrType = null;
        _state = FlowState.Returning;

        try
        {
            Await(_host.DelayAsync(_id, Consume(ref _delay), _cancellationToken));
        }
        catch (Exception ex)
        {
            _valueTaskSource.SetException(ex);
        }
    }

    private void Callback()
    {
        Debug.Assert(_callback is not null);
        _suspendingAwaiterOrType = null;
        _state = FlowState.Returning;

        try
        {
            Await(Consume(ref _callback).InvokeAsync(_id, _cancellationToken));
        }
        catch (Exception ex)
        {
            _valueTaskSource.SetException(ex);
        }
    }

    private delegate void Continuation(DAsyncFlow flow);
}
