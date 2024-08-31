using DTasks.Host;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DTasks;

internal abstract class DTaskBuilder<TResult> : DTask<TResult>
{
    private AsyncTaskMethodBuilder<bool> _underlyingTaskBuilder;
    private TResult? _result;
    private DTaskStatus _status;

    private DTaskBuilder()
    {
        _underlyingTaskBuilder = AsyncTaskMethodBuilder<bool>.Create();
        _status = DTaskStatus.Running;
    }

    public sealed override DTaskStatus Status => _status;

    internal sealed override TResult Result
    {
        get
        {
            VerifyStatus(DTaskStatus.RanToCompletion);
            return _result!;
        }
    }

    private protected sealed override Task<bool> UnderlyingTask => _underlyingTaskBuilder.Task;

    // The native AsyncTaskMethodBuilder implementation is not bound to a particular state
    // machine type, nor does it check whether their methods are passed the same state machine
    // each time as argument. Since they only use the state machine that was passed first,
    // passing different state machines to its methods leads to unexpected behavior.
    // Since we do the same, let's ensure we (and potential advanced users) always pass the
    // same state machine each time (in debug mode, the state machine is a reference type).
    [Conditional("DEBUG")]
    public abstract void EnsureSameStateMachine(IAsyncStateMachine stateMachine);

    public abstract void Start();

    public abstract void AwaitOnCompleted<TAwaiter>(ref TAwaiter awaiter)
        where TAwaiter : INotifyCompletion;

    public abstract void AwaitUnsafeOnCompleted<TAwaiter>(ref TAwaiter awaiter)
        where TAwaiter : ICriticalNotifyCompletion;

    public void SetResult(TResult result)
    {
        Debug.Assert(_status is DTaskStatus.Running, "The result of a DTask should be set when it is running.");

        _status = DTaskStatus.RanToCompletion;
        _result = result;

        // The underlying task must be completed after changing the status, so its continuations find this instance in a consistent state.
        _underlyingTaskBuilder.SetResult(true);
    }

    public void SetException(Exception exception)
    {
        throw new NotImplementedException();
    }

    private void Suspend()
    {
        _status = DTaskStatus.Suspended;

        // The underlying task must be completed after changing the status, so its continuations find this instance in a consistent state.
        _underlyingTaskBuilder.SetResult(false);
    }

    public static DTaskBuilder<TResult> Create<TStateMachine>(ref TStateMachine stateMachine)
      where TStateMachine : IAsyncStateMachine
    {
        return new DAsyncStateMachineBox<TStateMachine>(ref stateMachine);
    }

    private sealed class DAsyncStateMachineBox<TStateMachine> : DTaskBuilder<TResult>, ISuspensionInfo, IAsyncStateMachine
        where TStateMachine : IAsyncStateMachine
    {
        private TStateMachine _stateMachine;
        private DTask? _childTask;

        public DAsyncStateMachineBox(ref TStateMachine stateMachine)
        {
            _stateMachine = stateMachine;

            // Since we box the state machine right from the start, all MoveNext() invocations will be performed
            // on this copy, so we don't need the "mind-bending" procedure the native implementation does to
            // efficiently set up the builder. Here, we just complete the initialization of our boxed copy by calling
            // 'SetStateMachine' to ensure its AsyncDTaskMethodBuilder (which still has its default value) references
            // the current instance as its '_builder' field.
            // In release mode, this happens because the 'SetStateMachine' method on the generated state machine forwards
            // the call to its builder, hence we will end up calling our 'AsyncDTaskMethodBuilder.SetStateMachine'.
            // In debug mode, 'SetStateMachine' method on the generated state machine will do nothing, but that is not
            // a problem, since TStateMachine is a class in that case.
            _stateMachine.SetStateMachine(this);
        }

        internal override Task OnSuspendedAsync<THandler>(ref THandler handler, CancellationToken cancellationToken)
        {
            Debug.Assert(_childTask is not null, "The d-async method was not suspended.");

            handler.SaveStateMachine(ref _stateMachine, this);
            return _childTask.OnSuspendedAsync(ref handler, cancellationToken);
        }

        public override void Start()
        {
            DAsyncStateMachineBox<TStateMachine> box = this;
            _underlyingTaskBuilder.Start(ref box);
        }

        public override void AwaitOnCompleted<TAwaiter>(ref TAwaiter awaiter)
        {
            HandleOnCompleted(ref awaiter);

            DAsyncStateMachineBox<TStateMachine> box = this;
            _underlyingTaskBuilder.AwaitOnCompleted(ref awaiter, ref box);
        }

        public override void AwaitUnsafeOnCompleted<TAwaiter>(ref TAwaiter awaiter)
        {
            HandleOnCompleted(ref awaiter);

            DAsyncStateMachineBox<TStateMachine> box = this;
            _underlyingTaskBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref box);
        }

        private void HandleOnCompleted<TAwaiter>(ref TAwaiter awaiter)
        {
            _childTask = awaiter is IDTaskAwaiter
              ? ((IDTaskAwaiter)awaiter).Task
              : null;
        }

        public override void EnsureSameStateMachine(IAsyncStateMachine stateMachine)
        {
            Debug.Assert(ReferenceEquals(stateMachine, _stateMachine), "All methods of the AsyncDTaskMethodBuilder must be passed the same state machine.");
        }

        #region ISuspensionInfo implementation

        bool ISuspensionInfo.IsSuspended<TAwaiter>(ref TAwaiter awaiter)
        {
            Debug.Assert(_childTask is not null, "The d-async method was not suspended.");

            // Compiler-generated state machines use the same slots for different awaiters of the same type;
            // this is an implementation detail, but it is unlikely to be changed. Based on this fact,
            // we can determine which awaiter caused the suspension by looking at its type.
            // We could avoid taking the awaiter reference as an argument but leaking an implementation detail
            // through a public interface felt wrong.
            return typeof(TAwaiter) == _childTask.AwaiterType;
        }

        #endregion

        #region IAsyncStateMachine implementation

        void IAsyncStateMachine.MoveNext()
        {
            if (_childTask is not null && _childTask.IsSuspended)
            {
                // If the child awaitable is a d-awaitable and it is suspended, we must suspend our execution as well
                Suspend();
            }
            else
            {
                // Otherwise, we have a result and can move on with the caller method
                _stateMachine.MoveNext();
            }
        }

        [ExcludeFromCodeCoverage]
        void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
        {
            Debug.Fail("SetStateMachine should not be used.");
        }

        #endregion
    }
}
