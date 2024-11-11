using DTasks.Inspection;

namespace DTasks.Hosting;

internal partial class DAsyncFlow
{
    private void RunIndirection(Continuation continuation)
    {
        _continuation = continuation;
        _suspendingAwaiterOrType = typeof(HostIndirectionAwaiter);
        HostIndirectionStateMachine stateMachine = default;

        Dehydrate(_parentId, _id, ref stateMachine);
    }

    private struct HostIndirectionRunnableBuilder
    {
        public IDAsyncRunnable Task { get; private set; }

        public void Start(ref HostIndirectionStateMachine stateMachine)
        {
            Task = stateMachine.Awaiter.Runnable;
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
