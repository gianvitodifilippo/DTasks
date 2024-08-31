using DTasks.Host;

namespace DTasks;

public sealed class DTaskFactory
{
    public static readonly DTaskFactory Instance = new();

    private DTaskFactory() { }

    public DTask Suspend(SuspensionCallback callback) => new DelegateSuspendedDTask<VoidDTaskResult>(callback);

    public DTask<TResult> Suspend<TResult>(SuspensionCallback callback) => new DelegateSuspendedDTask<TResult>(callback);

    public DTask Suspend(ISuspensionCallback callback) => new SuspendedDTask<VoidDTaskResult>(callback);

    public DTask<TResult> Suspend<TResult>(ISuspensionCallback callback) => new SuspendedDTask<TResult>(callback);

    private sealed class DelegateSuspendedDTask<TResult>(SuspensionCallback callback) : DTask<TResult>, ISuspensionCallback
    {
        public override DTaskStatus Status => DTaskStatus.Suspended;

        internal override TResult Result
        {
            get
            {
                InvalidStatus(DTaskStatus.RanToCompletion);
                return default!;
            }
        }

        private protected override Task<bool> UnderlyingTask => Task.FromResult(false);

        internal override Task OnSuspendedAsync<THandler>(ref THandler handler, CancellationToken cancellationToken)
        {
            return handler.OnSuspendedAsync(this, cancellationToken);
        }

        Task ISuspensionCallback.OnSuspendedAsync<TFlowId>(TFlowId flowId, CancellationToken cancellationToken)
        {
            return callback(flowId, cancellationToken);
        }
    }
}
