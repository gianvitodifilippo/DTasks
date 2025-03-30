using DTasks.Infrastructure;
using DTasks.Inspection;
using DTasks.Marshaling;
using DTasks.Utils;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DTasks.Infrastructure;

internal partial class DAsyncFlow
{
    private sealed class HandleRunnableWrapper(DAsyncFlow flow, IDAsyncRunnable runnable, DAsyncId id) : IDAsyncRunnable, IDAsyncStateMachine
    {
        public void Run(IDAsyncRunner runner)
        {
            Debug.Assert(ReferenceEquals(runner, flow));
            runner.Start(this);
        }

        void IDAsyncStateMachine.Start(IDAsyncMethodBuilder builder)
        {
            Debug.Assert(ReferenceEquals(builder, flow));
        }

        void IDAsyncStateMachine.MoveNext()
        {
        }

        void IDAsyncStateMachine.Suspend()
        {
            HandleStateMachine stateMachine = default;

            DAsyncId parentId = flow._parentId;

            flow._parentId = id;
            flow._continuation = Continuations.HandleWrapper;
            flow._aggregateRunnable = runnable;
            flow._suspendingAwaiterOrType = typeof(HandleAwaiter);
            flow.Dehydrate(parentId, id, ref stateMachine);
        }
    }

    private class HandleRunnable : IDAsyncRunnable
    {
        public virtual void Run(IDAsyncRunner runner)
        {
            if (runner is not DAsyncFlow flow)
                throw new ArgumentException("A d-async runnable was resumed on a different flow than the one that started it.");

            DAsyncId id = flow._id;
            CompletedHandleStateMachine stateMachine = default;
            stateMachine.Runnable = this;
            flow._suspendingAwaiterOrType = typeof(CompletedHandleAwaiter);
            flow._continuation = self => // TODO: Try avoid this allocation
            {
                self._tasks.Add(id, DTask.CompletedDTask);
                self.Resume(self._parentId);
            };
            flow.Dehydrate(default, id, ref stateMachine);
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
        public override void Run(IDAsyncRunner runner)
        {
            if (runner is not DAsyncFlow flow)
                throw new ArgumentException("A d-async runnable was resumed on a different flow than the one that started it.");

            DAsyncId id = flow._id;
            CompletedHandleStateMachine stateMachine = default;
            stateMachine.Runnable = this;
            flow._suspendingAwaiterOrType = typeof(CompletedHandleAwaiter);
            flow._continuation = self => // TODO: Try avoid this allocation
            {
                self._tasks.Add(id, DTask.FromResult(result));
                self.Resume(self._parentId, result);
            };
            flow.Dehydrate(default, flow._id, ref stateMachine);
        }

        public override void Write<T, TAction>(scoped ref TAction action)
        {
            action.MarshalAs(default, result);
        }
    }

    private struct HandleRunnableBuilder
    {
        public IDAsyncRunnable Task { get; private set; }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
        {
            // TODO: Inspector should support non-generic start method
            Assert.Is<HandleStateMachine>(stateMachine);

            Task = Unsafe.As<TStateMachine, HandleStateMachine>(ref stateMachine).Awaiter.Runnable;
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
        public override void Run(IDAsyncRunner runner)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class CompletedHandleRunnable<TResult>(TResult result) : CompletedHandleRunnable
    {
        public override void Run(IDAsyncRunner runner)
        {
            if (runner is not DAsyncFlow flow)
                throw new ArgumentException("A d-async runnable was resumed on a different flow than the one that started it.");

            IDAsyncStateMachine? stateMachine = flow._stateMachine;
            object? resultBuilder = flow.Consume(ref flow._resultBuilder);

            Assert.NotNull(stateMachine);
            Assert.Is<IDAsyncResultBuilder<TResult>>(resultBuilder);
            
            Unsafe.As<IDAsyncResultBuilder<TResult>>(resultBuilder).SetResult(result);
            stateMachine.MoveNext();
        }
    }

    private struct CompletedHandleRunnableBuilder
    {
        public IDAsyncRunnable Task { get; private set; }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
        {
            // TODO: Inspector should support non-generic start method
            Assert.Is<CompletedHandleStateMachine>(stateMachine);

            Task = Unsafe.As<TStateMachine, CompletedHandleStateMachine>(ref stateMachine).Runnable;
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
