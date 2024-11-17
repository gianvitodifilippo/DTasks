using DTasks.Inspection;
using DTasks.Marshaling;
using DTasks.Utils;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DTasks.Hosting;

internal partial class DAsyncFlow
{
    private sealed class HandleRunnableWrapper(DAsyncFlow flowImpl, IDAsyncRunnable runnable, DAsyncId id) : IDAsyncRunnable, IDAsyncStateMachine
    {
        public void Run(IDAsyncFlow flow)
        {
            Debug.Assert(ReferenceEquals(flow, flowImpl));
            flow.Start(this);
        }

        void IDAsyncStateMachine.Start(IDAsyncMethodBuilder builder)
        {
            Debug.Assert(ReferenceEquals(builder, flowImpl));
        }

        void IDAsyncStateMachine.MoveNext()
        {
        }

        void IDAsyncStateMachine.Suspend()
        {
            HandleStateMachine stateMachine = default;

            DAsyncId parentId = flowImpl._parentId;

            flowImpl._parentId = id;
            flowImpl._continuation = Continuations.HandleWrapper;
            flowImpl._aggregateRunnable = runnable;
            flowImpl._suspendingAwaiterOrType = typeof(HandleAwaiter);
            flowImpl.Dehydrate(parentId, id, ref stateMachine);
        }
    }

    private class HandleRunnable : IDAsyncRunnable
    {
        public virtual void Run(IDAsyncFlow flow)
        {
            if (flow is not DAsyncFlow flowImpl)
                throw new ArgumentException("A d-async runnable was resumed on a different flow than the one that started it.");

            CompletedHandleStateMachine stateMachine = default;
            stateMachine.Runnable = this;
            flowImpl._suspendingAwaiterOrType = typeof(CompletedHandleAwaiter);
            flowImpl._continuation = self => self.Resume(self._parentId);
            flowImpl.Dehydrate(default, flowImpl._id, ref stateMachine);
        }

        public virtual void Write<T, TAction>(scoped ref TAction action)
            where TAction : IMarshalingAction
#if NET9_0_OR_GREATER
            , allows ref struct
#endif
        {
            action.MarshalAs(default, null as object);
        }
    }

    private sealed class HandleRunnable<TResult>(TResult result) : HandleRunnable
    {
        public override void Run(IDAsyncFlow flow)
        {
            if (flow is not DAsyncFlow flowImpl)
                throw new ArgumentException("A d-async runnable was resumed on a different flow than the one that started it.");

            CompletedHandleStateMachine stateMachine = default;
            stateMachine.Runnable = this;
            flowImpl._suspendingAwaiterOrType = typeof(CompletedHandleAwaiter);
            flowImpl._continuation = self => self.Resume(self._parentId, result); // TODO: Avoid this allocation
            flowImpl.Dehydrate(default, flowImpl._id, ref stateMachine);
        }

        public override void Write<T, TAction>(scoped ref TAction action)
        {
            action.MarshalAs(default, result);
        }
    }

    private struct HandleRunnableBuilder
    {
        public IDAsyncRunnable Task { get; private set; }

        public void Start(ref HandleStateMachine stateMachine)
        {
            Task = stateMachine.Awaiter.Runnable;
        }

        public static HandleRunnableBuilder Create() => default;
    }

    private readonly struct HandleAwaiter(HandleRunnable runnable)
    {
        public HandleRunnable Runnable => runnable;

        public static HandleAwaiter FromResult() => new(new HandleRunnable());

        public static HandleAwaiter FromResult<TResult>(TResult result) => new(new HandleRunnable<TResult>(result));

        public static HandleAwaiter FromException(Exception exception) => throw new NotImplementedException();
    }

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    private struct HandleStateMachine
    {
        [DAsyncRunnableBuilderField]
        public HandleRunnableBuilder Builder;

        [DAsyncAwaiterField]
        public HandleAwaiter Awaiter;
    }
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value


    private class CompletedHandleRunnable : HandleRunnable
    {
        public override void Run(IDAsyncFlow flow)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class CompletedHandleRunnable<TResult>(TResult result) : CompletedHandleRunnable
    {
        public override void Run(IDAsyncFlow flow)
        {
            if (flow is not DAsyncFlow flowImpl)
                throw new ArgumentException("A d-async runnable was resumed on a different flow than the one that started it.");

            IDAsyncStateMachine? stateMachine = flowImpl._stateMachine;
            object? resultBuilder = flowImpl.Consume(ref flowImpl._resultBuilder);

            Assert.NotNull(stateMachine);
            Assert.Is<IDAsyncResultBuilder<TResult>>(resultBuilder);
            
            Unsafe.As<IDAsyncResultBuilder<TResult>>(resultBuilder).SetResult(result);
            stateMachine.MoveNext();
        }
    }

    private struct CompletedHandleRunnableBuilder
    {
        public IDAsyncRunnable Task { get; private set; }

        public void Start(ref CompletedHandleStateMachine stateMachine)
        {
            Task = stateMachine.Runnable;
        }

        public static CompletedHandleRunnableBuilder Create() => default;
    }

    private readonly struct CompletedHandleAwaiter
    {
        public static CompletedHandleAwaiter FromResult() => new();

        public static CompletedHandleAwaiter FromException(Exception exception) => throw new NotImplementedException();
    }

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    private struct CompletedHandleStateMachine
    {
        [DAsyncRunnableBuilderField]
        public CompletedHandleRunnableBuilder Builder;

        [DAsyncAwaiterField]
        public CompletedHandleAwaiter Awaiter;

        public HandleRunnable Runnable;
    }
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
}
