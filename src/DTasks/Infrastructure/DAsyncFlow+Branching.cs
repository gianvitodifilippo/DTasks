using DTasks.Utils;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow
{
    private void PushNode<TResultBuilder>(TResultBuilder resultBuilder, NodeBuilder<TResultBuilder> nodeBuilder)
        where TResultBuilder : class
    {
        Assert.Equal(FlowState.Running, _state);

        DAsyncId id = Consume(ref _id);
        
        NodeProperties.Push(new FlowNode(
            id,
            Consume(ref _parentId),
            Consume(ref _stateMachine),
            Consume(ref _suspendingAwaiterOrType),
            Consume(ref _nodeId),
            Consume(ref _nodeResultBuilder),
            Consume(ref _nodeBuilder)));

        if (nodeBuilder.HasParent)
        {
            _parentId = id;
        }
        
        _id = _idFactory.NewId();
        _nodeId = _id;
        _nodeResultBuilder = resultBuilder;
        _nodeBuilder = nodeBuilder;
    }

    private void PopNode()
    {
        Assert.NotNull(_nodes);
        
        FlowNode flowNode = _nodes.Pop();
        _state = FlowState.Running;
        _id = flowNode.Id;
        _parentId = flowNode.ParentId;
        Assign(ref _stateMachine, flowNode.StateMachine);
        Assign(ref _suspendingAwaiterOrType, flowNode.SuspendedAwaiterOrType);
        AssignStruct(ref _nodeId, flowNode.NodeId);
        Assign(ref _nodeResultBuilder, flowNode.NodeResultBuilder);
        Assign(ref _nodeBuilder, flowNode.NodeResultHandler);
    }

    private readonly record struct FlowNode(
        DAsyncId Id,
        DAsyncId ParentId,
        IDAsyncStateMachine? StateMachine,
        object? SuspendedAwaiterOrType,
        DAsyncId NodeId,
        object? NodeResultBuilder,
        INodeBuilder? NodeResultHandler);
    
    private interface INodeBuilder
    {
        void SetResult(DAsyncFlow flow);
        
        void SetResult<TResult>(DAsyncFlow flow, TResult result);
        
        void SetException(DAsyncFlow flow, Exception exception);
        
        void Suspend(DAsyncFlow flow);
    }
    
    private abstract class NodeBuilder<TResultBuilder> : INodeBuilder
        where TResultBuilder : class
    {
        public abstract bool HasParent { get; }
        
        protected abstract void SetResult(DAsyncFlow flow, TResultBuilder resultBuilder);

        protected abstract void SetResult<TResult>(DAsyncFlow flow, TResultBuilder resultBuilder, TResult result);

        protected abstract void SetException(DAsyncFlow flow, TResultBuilder resultBuilder, Exception exception);

        protected abstract void Suspend(DAsyncFlow flow, TResultBuilder resultBuilder, DAsyncId nodeId);

        public void SetResult(DAsyncFlow flow)
        {
            TResultBuilder resultBuilder = ConsumeResultBuilder(flow);
            flow._nodeBuilder = null;
            flow._nodeId = default;
            flow.PopNode();
            
            SetResult(flow, resultBuilder);
        }
        
        public void SetResult<TResult>(DAsyncFlow flow, TResult result)
        {
            TResultBuilder resultBuilder = ConsumeResultBuilder(flow);
            flow._nodeBuilder = null;
            flow._nodeId = default;
            flow.PopNode();
            
            SetResult(flow, resultBuilder, result);
        }
        
        public void SetException(DAsyncFlow flow, Exception exception)
        {
            TResultBuilder resultBuilder = ConsumeResultBuilder(flow);
            flow._nodeBuilder = null;
            flow._nodeId = default;
            flow.PopNode();

            SetException(flow, resultBuilder, exception);
        }

        public void Suspend(DAsyncFlow flow)
        {
            TResultBuilder resultBuilder = ConsumeResultBuilder(flow);
            DAsyncId nodeId = Consume(ref flow._nodeId);
            flow._nodeBuilder = null;
            flow.PopNode();
            
            Suspend(flow, resultBuilder, nodeId);
        }

        private static TResultBuilder ConsumeResultBuilder(DAsyncFlow flow)
        {
            object resultBuilder = ConsumeNotNull(ref flow._nodeResultBuilder);
            return Reinterpret.Cast<TResultBuilder>(resultBuilder);
        }
    }
}