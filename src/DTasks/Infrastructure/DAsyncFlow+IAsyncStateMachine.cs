using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using DTasks.Marshaling;
using DTasks.Utils;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IAsyncStateMachine
{
    void IAsyncStateMachine.MoveNext()
    {
        try
        {
            switch (_state)
            {
                case FlowState.Starting:
                    MoveNextOnStart();
                    break;

                case FlowState.Running:
                    MoveNextOnRun();
                    break;

                case FlowState.Dehydrating:
                    MoveNextOnDehydrate();
                    break;

                case FlowState.Hydrating:
                    MoveNextOnHydrate();
                    break;
                
                case FlowState.Linking:
                    MoveNextOnLink();
                    break;

                case FlowState.Suspending:
                    MoveNextOnSuspend();
                    break;

                case FlowState.Terminating:
                    MoveNextOnReturn();
                    break;

                case FlowState.Flushing:
                    MoveNextOnFlush();
                    break;

                default:
                    Debug.Fail($"Unexpected state on MoveNext: {_state}.");
                    break;
            }
        }
        catch (MarshalingException ex)
        {
            _hasError = true;
            _valueTaskSource.SetException(ex);
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }
    }

    private void MoveNextOnStart()
    {
        GetVoidTaskResult();
        object? resultOrException = Consume(ref _resultOrException);
        IDAsyncRunnable runnable = ConsumeNotNull(ref _runnable);
        
        if (resultOrException is null)
        {
            runnable.Run(this);
            return;
        }
        
        if (resultOrException is Exception exception)
        {
            _valueTaskSource.SetException(exception);
            return;
        }

        Debug.Assert(resultOrException == s_resultSentinel, """
            _resultOrException may only be:
            - s_resultSentinel: when the host called SetResult()
            - an exception: when the host called SetException()
            - null: otherwise
            """);

        _valueTaskSource.SetResult(default);
    }

    private void MoveNextOnRun()
    {
        Assert.NotNull(_stateMachine);
        
        _stateMachine.MoveNext();
    }

    private void MoveNextOnDehydrate()
    {
        GetVoidValueTaskResult();
        DehydrateContinuation continuation = ConsumeNotNull(ref _dehydrateContinuation);
        _suspendingAwaiterOrType = null;

        continuation.Invoke(this);
    }

    private void MoveNextOnHydrate()
    {
        (_parentId, IDAsyncRunnable runnable) = GetLinkValueTaskResult();
        
        runnable.Run(this);
    }

    private void MoveNextOnLink()
    {
        GetVoidValueTaskResult();

        if (_handleResultHandler is null)
        {
            Assert.NotNull(_stateMachine);
            
            _state = FlowState.Running;
            _suspendingAwaiterOrType = null;
            _handleId = default;
            _stateMachine.MoveNext();
            return;
        }

        _handleResultHandler = null;
        _resultBuilder = null;
        _handleId = default;
        _state = FlowState.Running;
        Suspend(static self => self.AwaitOnSuspend());
    }

    private void MoveNextOnSuspend()
    {
        GetVoidTaskResult();

        if (TryPopNode(out DAsyncId childId, out INodeResultHandler? resultHandler))
        {
            resultHandler.Suspend(this, childId);
            Continue();
            return;
        }
        
        AwaitOnSuspend();
    }

    private void MoveNextOnReturn()
    {
        GetVoidTaskResult();

        AwaitFlush();
    }

    private void MoveNextOnFlush()
    {
        GetVoidValueTaskResult();
        
        _valueTaskSource.SetResult(default);
    }

    [ExcludeFromCodeCoverage]
    void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
    {
        ThrowHelper.ThrowIfNull(stateMachine);

        Debug.Fail("SetStateMachine should not be used.");
    }

    private enum FlowState
    {
        Idling, // In the pool
        Pending, // Leased, waiting for runnable
        Starting, // Starting the runnable
        Running, // Executing the runnable
        Dehydrating, // Awaiting DehydrateAsync
        Hydrating, // Awaiting HydrateAsync
        Linking, // Awaiting LinkAsync
        Suspending, // Awaiting suspension callback
        Terminating, // Awaiting a termination hook on the host
        Flushing, // Awaiting FlushAsync
        Branching, // Running a branch of the flow TODO: Not using it after await
        // Aggregating, // Running multiple aggregated runnables
        // Awaiting // Awaiting a custom task
    }
}
