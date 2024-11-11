using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using DTasks.Utils;

namespace DTasks.Hosting;

internal partial class DAsyncFlow : IAsyncStateMachine
{
    void IAsyncStateMachine.MoveNext()
    {
        try
        {
            switch (_state)
            {
                case FlowState.Running: // After awaiting a regular awaitable or a completed d-awaitable
                    Debug.Assert(_stateMachine is not null);

                    _stateMachine.MoveNext();
                    break;

                case FlowState.Dehydrating:
                    Debug.Assert(_continuation is not null);

                    GetVoidValueTaskResult();
                    _suspendingAwaiterOrType = null;
                    Consume(ref _continuation).Invoke(this);
                    break;

                case FlowState.Hydrating: // After calling _stateManager.HydrateAsync
                    (_parentId, IDAsyncRunnable runnable) = GetLinkValueTaskResult();
                    runnable.Run(this);
                    break;
                    
                case FlowState.Returning:
                    GetVoidTaskResult();
                    _valueTaskSource.SetResult(default);
                    break;

                default:
                    Debug.Fail($"Invalid state after await: '{_state}'.");
                    break;
            }
        }
        catch (Exception ex)
        {
            _valueTaskSource.SetException(ex);
        }
    }

    [ExcludeFromCodeCoverage]
    void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
    {
        ThrowHelper.ThrowIfNull(stateMachine);

        Debug.Fail("SetStateMachine should not be used.");
    }

    private enum FlowState
    {
        Pending,
        Running,
        Dehydrating,
        Hydrating,
        Returning
    }
}
