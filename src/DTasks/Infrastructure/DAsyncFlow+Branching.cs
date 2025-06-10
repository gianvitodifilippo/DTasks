using System.Diagnostics.CodeAnalysis;
using DTasks.Utils;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow
{
    private void PushNode(object resultBuilder, INodeResultHandler resultHandler)
    {
        DAsyncId childId = _idFactory.NewId();
        
        NodeProperties.Push(new DAsyncNodeProperties(
            childId,
            Consume(ref _id),
            Consume(ref _parentId),
            Consume(ref _stateMachine),
            Consume(ref _suspendingAwaiterOrType),
            resultBuilder,
            resultHandler));

        _id = childId;
    }

    private bool TryPopNode(out DAsyncId childId, [NotNullWhen(true)] out INodeResultHandler? resultHandler)
    {
        if (_nodeProperties is null || _nodeProperties.Count == 0)
        {
            childId = default;
            resultHandler = null;
            return false;
        }
        
        DAsyncNodeProperties nodeProperties = _nodeProperties.Pop();
        _id = nodeProperties.Id;
        _parentId = nodeProperties.ParentId;
        _state = FlowState.Running;
        Assign(ref _stateMachine, nodeProperties.StateMachine);
        Assign(ref _suspendingAwaiterOrType, nodeProperties.SuspendedAwaiterOrType);
        Assign(ref _resultBuilder, nodeProperties.ResultBuilder);

        childId = nodeProperties.ChildId;
        resultHandler = nodeProperties.ResultHandler;
        return true;
    }
    
    private interface INodeResultHandler
    {
        void SetResult(DAsyncFlow flow);
        
        void SetResult<TResult>(DAsyncFlow flow, TResult result);
        
        void SetException(DAsyncFlow flow, Exception exception);
        
        void Suspend(DAsyncFlow flow, DAsyncId childId);
    }

    private sealed class RunBackgroundNodeResultHandler : INodeResultHandler
    {
        public static readonly RunBackgroundNodeResultHandler Instance = new();

        private RunBackgroundNodeResultHandler()
        {
        }
        
        public void SetResult(DAsyncFlow flow)
        {
            IDAsyncResultBuilder<DTask> resultBuilder = ConsumeResultBuilder(flow);
            flow._suspendingAwaiterOrType = null;
            resultBuilder.SetResult(DTask.CompletedDTask);
        }

        public void SetResult<TResult>(DAsyncFlow flow, TResult result)
        {
            IDAsyncResultBuilder<DTask> resultBuilder = ConsumeResultBuilder(flow);
            flow._suspendingAwaiterOrType = null;
            resultBuilder.SetResult(DTask.FromResult(result));
        }

        public void SetException(DAsyncFlow flow, Exception exception)
        {
            IDAsyncResultBuilder<DTask> resultBuilder = ConsumeResultBuilder(flow);
            flow._suspendingAwaiterOrType = null;
            resultBuilder.SetResult(DTask.FromException(exception));
        }

        public void Suspend(DAsyncFlow flow, DAsyncId childId)
        {
            IDAsyncResultBuilder<DTask> resultBuilder = ConsumeResultBuilder(flow);
            DTaskHandle handle = new(childId);
            flow._suspendingAwaiterOrType = null;
            flow.HandleIds.Add(handle, childId);
            resultBuilder.SetResult(handle);
        }

        private static IDAsyncResultBuilder<DTask> ConsumeResultBuilder(DAsyncFlow flow)
        {
            object resultBuilder = ConsumeNotNull(ref flow._resultBuilder);
            return Reinterpret.Cast<IDAsyncResultBuilder<DTask>>(resultBuilder);
        }
    }

    private sealed class RunBackgroundNodeResultHandler<TNodeResult> : INodeResultHandler
    {
        public static readonly RunBackgroundNodeResultHandler<TNodeResult> Instance = new();

        private RunBackgroundNodeResultHandler()
        {
        }

        public void SetResult(DAsyncFlow flow)
        {
            _ = ConsumeResultBuilder(flow);
            flow._suspendingAwaiterOrType = null;

            throw new InvalidOperationException($"Node should have been resumed with result of type '{typeof(TNodeResult).FullName}'.");
        }

        public void SetResult<TResult>(DAsyncFlow flow, TResult result)
        {
            IDAsyncResultBuilder<DTask<TNodeResult>> resultBuilder = ConsumeResultBuilder(flow);
            flow._suspendingAwaiterOrType = null;
            
            if (result is not TNodeResult nodeResult)
                throw new InvalidOperationException($"Node should have been resumed with result of type '{typeof(TNodeResult).FullName}'.");
            
            resultBuilder.SetResult(DTask.FromResult(nodeResult));
        }

        public void SetException(DAsyncFlow flow, Exception exception)
        {
            IDAsyncResultBuilder<DTask<TNodeResult>> resultBuilder = ConsumeResultBuilder(flow);
            flow._suspendingAwaiterOrType = null;
            resultBuilder.SetResult(DTask<TNodeResult>.FromException(exception));
        }

        public void Suspend(DAsyncFlow flow, DAsyncId childId)
        {
            IDAsyncResultBuilder<DTask<TNodeResult>> resultBuilder = ConsumeResultBuilder(flow);
            DTaskHandle<TNodeResult> handle = new(childId);
            flow._suspendingAwaiterOrType = null;
            flow.HandleIds.Add(handle, childId);
            resultBuilder.SetResult(handle);
        }

        private static IDAsyncResultBuilder<DTask<TNodeResult>> ConsumeResultBuilder(DAsyncFlow flow)
        {
            object resultBuilder = ConsumeNotNull(ref flow._resultBuilder);
            return Reinterpret.Cast<IDAsyncResultBuilder<DTask<TNodeResult>>>(resultBuilder);
        }
    }
}