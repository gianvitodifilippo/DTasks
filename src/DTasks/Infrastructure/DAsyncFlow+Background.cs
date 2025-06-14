using System.Diagnostics.CodeAnalysis;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow
{
    private sealed class BackgroundNodeBuilder : NodeBuilder<IDAsyncResultBuilder<DTask>>
    {
        public static readonly BackgroundNodeBuilder Instance = new();

        private BackgroundNodeBuilder()
        {
        }

        public override bool HasParent => false;

        protected override void SetResult(DAsyncFlow flow, IDAsyncResultBuilder<DTask> resultBuilder)
        {
            flow._frameHasIds = false;
            flow._suspendingAwaiterOrType = null;
            
            resultBuilder.SetResult(DTask.CompletedDTask);
            flow.Continue();
        }

        protected override void SetResult<TResult>(DAsyncFlow flow, IDAsyncResultBuilder<DTask> resultBuilder, TResult result)
        {
            flow._frameHasIds = false;
            flow._suspendingAwaiterOrType = null;
            
            resultBuilder.SetResult(DTask.FromResult(result));
            flow.Continue();
        }

        protected override void SetException(DAsyncFlow flow, IDAsyncResultBuilder<DTask> resultBuilder, Exception exception)
        {
            flow._frameHasIds = false;
            flow._suspendingAwaiterOrType = null;
            
            resultBuilder.SetResult(DTask.FromException(exception));
            flow.Continue();
        }

        protected override void Suspend(DAsyncFlow flow, IDAsyncResultBuilder<DTask> resultBuilder, DAsyncId nodeId)
        {
            flow._suspendingAwaiterOrType = null;
            flow._nodeBuilder = null;
            
            DTaskHandle handle = new(nodeId);
            flow.HandleIds.Add(handle, nodeId);
            resultBuilder.SetResult(handle);
            flow.Continue();
        }
    }

    private sealed class BackgroundNodeBuilder<TNodeResult> : NodeBuilder<IDAsyncResultBuilder<DTask<TNodeResult>>>
    {
        public static readonly BackgroundNodeBuilder<TNodeResult> Instance = new();

        private BackgroundNodeBuilder()
        {
        }

        public override bool HasParent => false;

        protected override void SetResult(DAsyncFlow flow, IDAsyncResultBuilder<DTask<TNodeResult>> resultBuilder)
        {
            flow._frameHasIds = false;
            flow._suspendingAwaiterOrType = null;
            
            ThrowWrongResultType();
        }

        protected override void SetResult<TResult>(DAsyncFlow flow, IDAsyncResultBuilder<DTask<TNodeResult>> resultBuilder, TResult result)
        {
            flow._frameHasIds = false;
            flow._suspendingAwaiterOrType = null;
            
            if (result is not TNodeResult nodeResult)
            {
                ThrowWrongResultType();
                return;
            }
            
            resultBuilder.SetResult(DTask.FromResult(nodeResult));
            flow.Continue();
        }

        protected override void SetException(DAsyncFlow flow, IDAsyncResultBuilder<DTask<TNodeResult>> resultBuilder, Exception exception)
        {
            flow._frameHasIds = false;
            flow._suspendingAwaiterOrType = null;
            
            resultBuilder.SetResult(DTask<TNodeResult>.FromException(exception));
            flow.Continue();
        }

        protected override void Suspend(DAsyncFlow flow, IDAsyncResultBuilder<DTask<TNodeResult>> resultBuilder, DAsyncId nodeId)
        {
            flow._suspendingAwaiterOrType = null;
            flow._nodeBuilder = null;
            
            DTaskHandle<TNodeResult> handle = new(nodeId);
            flow.HandleIds.Add(handle, nodeId);
            resultBuilder.SetResult(handle);
            flow.Continue();
        }

        [DoesNotReturn]
        private static void ThrowWrongResultType()
        {
            throw new InvalidOperationException($"Node should have been resumed with result of type '{typeof(TNodeResult).FullName}'.");
        }
    }
}