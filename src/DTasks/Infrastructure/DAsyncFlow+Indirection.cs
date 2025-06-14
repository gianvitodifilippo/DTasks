using System.Runtime.CompilerServices;
using DTasks.Inspection;
using DTasks.Utils;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow
{
    // Indirections is the way we hide the id of a d-async method from the outside.
    // If we let outside the id it was assigned, it could potentially be used to resume execution
    // many times, instead of just once. Instead, we generate a once-only id which is exposed,
    // e.g., through SuspensionHandler.Yield. This id refers to a state machine containing no fields,
    // but just a link to its parent, the original d-async state machine that was suspended.

    private void RunYieldIndirection()
    {
        RunIndirection(static self => self.AwaitOnYield());
    }

    private void RunDelayIndirection()
    {
        RunIndirection(static self => self.AwaitOnDelay());
    }

    private void RunIndirection(DehydrateContinuation continuation)
    {
        Assign(ref _dehydrateContinuation, continuation);
        Assign(ref _suspendingAwaiterOrType, typeof(IndirectionAwaiter));
        IndirectionStateMachine stateMachine = default;
        StartFrame();
        
        AwaitDehydrate(ref stateMachine);
    }
    
    private struct IndirectionRunnableBuilder
    {
        public IDAsyncRunnable Task { get; private set; }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
        {
            // TODO: Inspector should support non-generic start method
            Assert.Is<IndirectionStateMachine>(stateMachine);

            Task = Unsafe.As<TStateMachine, IndirectionStateMachine>(ref stateMachine).Awaiter.Runnable;
        }

        public static IndirectionRunnableBuilder Create() => default;
    }

    private readonly struct IndirectionAwaiter(IDAsyncRunnable runnable)
    {
        public IDAsyncRunnable Runnable => runnable;

        public static IndirectionAwaiter FromResult() => new(DTask.CompletedDTask);

        public static IndirectionAwaiter FromResult<TResult>(TResult result) => new(DTask.FromResult(result));

        public static IndirectionAwaiter FromException(Exception exception) => new(DTask.FromException(exception));
    }

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    private struct IndirectionStateMachine
    {
        [DAsyncRunnableBuilderField]
        public IndirectionRunnableBuilder Builder;

        [DAsyncAwaiterField]
        public IndirectionAwaiter Awaiter;
    }
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
}