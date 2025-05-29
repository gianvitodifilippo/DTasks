using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using DTasks.Infrastructure.State;
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
                
                case FlowState.Dehydrating: // Right now we handle indirections, might need a new state when handling nested methods
                    MoveNextOnDehydrate();
                    break;
                
                case FlowState.Hydrating:
                    MoveNextOnHydrate();
                    break;
                
                case FlowState.Suspending:
                    MoveNextOnSuspending();
                    break;
                
                case FlowState.Returning:
                    MoveNextOnReturning();
                    break;

                default:
                    Debug.Fail($"Unexpected state on MoveNext: {_state}.");
                    break;
            }
        }
        catch (Exception ex)
        {
            SetInfrastructureException(ex);
        }
    }

    private void MoveNextOnStart()
    {
        GetVoidTaskResult();
        object? resultOrException = Consume(ref _resultOrException);
        IDAsyncRunnable runnable = ConsumeNotNull(ref _runnable);
        
        if (resultOrException is null)
        {
            Run(runnable);
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
        IndirectionContinuation continuation = ConsumeNotNull(ref _continuation);
        _suspendingAwaiterOrType = null;

        continuation.Invoke(this);
    }

    private void MoveNextOnHydrate()
    {
        (_parentId, IDAsyncRunnable runnable) = GetLinkValueTaskResult();
        
        Run(runnable);
    }

    private void MoveNextOnSuspending()
    {
        GetVoidTaskResult();
        
        AwaitOnSuspend();
    }

    private void MoveNextOnReturning()
    {
        GetVoidTaskResult();
        
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
        Suspending, // Awaiting suspension callback
        Returning, // Returning from the runnable
        // Aggregating, // Running multiple aggregated runnables
        // Awaiting // Awaiting a custom task
    }
}
