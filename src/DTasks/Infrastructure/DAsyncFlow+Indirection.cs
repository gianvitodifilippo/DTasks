using DTasks.Inspection;
using DTasks.Utils;
using System.Runtime.CompilerServices;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow
{
    private void RunIndirection(FlowContinuation continuation)
    {
        _continuation = continuation;
        _suspendingAwaiterOrType = typeof(HostIndirectionAwaiter);
        HostIndirectionStateMachine stateMachine = default;

        Dehydrate(_parentId, _id, ref stateMachine);
    }

    private struct HostIndirectionRunnableBuilder
    {
        public IDAsyncRunnable Task { get; private set; }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
        {
            // TODO: Inspector should support non-generic start method
            Assert.Is<HostIndirectionStateMachine>(stateMachine);

            Task = Unsafe.As<TStateMachine, HostIndirectionStateMachine>(ref stateMachine).Awaiter.Runnable;
        }

        public static HostIndirectionRunnableBuilder Create() => default;
    }

    private readonly struct HostIndirectionAwaiter(IDAsyncRunnable runnable)
    {
        public IDAsyncRunnable Runnable => runnable;

        public static HostIndirectionAwaiter FromResult() => new(DTask.CompletedDTask);

        public static HostIndirectionAwaiter FromResult<TResult>(TResult result) => new(DTask.FromResult(result));

        public static HostIndirectionAwaiter FromException(Exception exception) => new(DTask.FromException(exception));
    }

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    private struct HostIndirectionStateMachine
    {
        [DAsyncRunnableBuilderField]
        public HostIndirectionRunnableBuilder Builder;

        [DAsyncAwaiterField]
        public HostIndirectionAwaiter Awaiter;
    }
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
}
