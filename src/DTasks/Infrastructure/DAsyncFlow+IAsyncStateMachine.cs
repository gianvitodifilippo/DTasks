using DTasks.Utils;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DTasks.Infrastructure;

internal partial class DAsyncFlow : IAsyncStateMachine
{
    void IAsyncStateMachine.MoveNext()
    {
        try
        {
            switch (_state)
            {
                case FlowState.Running: // After awaiting a regular awaitable or a completed d-awaitable
                    Assert.NotNull(_stateMachine);

                    _stateMachine.MoveNext();
                    break;

                case FlowState.Dehydrating: // After awaiting _stateManager.DehydrateAsync
                    Assert.NotNull(_continuation);

                    GetVoidValueTaskResult();
                    _suspendingAwaiterOrType = null;
                    _state = FlowState.Running;
                    Consume(ref _continuation).Invoke(this);
                    break;

                case FlowState.Hydrating: // After awaiting _stateManager.HydrateAsync
                    (DAsyncId parentId, IDAsyncRunnable runnable) = GetLinkValueTaskResult();
                    if (parentId != default)
                    {
                        _parentId = parentId; // TODO: Remove this and implement a better solution. It is like this only to support handles
                    }

                    _state = FlowState.Running;
                    runnable.Run(this);
                    break;

                case FlowState.Returning: // After awaiting a method that results in completing the d-async flow
                    GetVoidTaskResult();
                    _valueTaskSource.SetResult(default);
                    break;

                case FlowState.Aggregating: // After awaiting WhenAllAsync, WhenAnyAsync, etc
                    Assert.NotNull(_aggregateRunnable);

                    GetVoidTaskResult();
                    _state = FlowState.Running;
                    Consume(ref _aggregateRunnable).Run(this);
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
        Returning,
        Aggregating
    }
}
