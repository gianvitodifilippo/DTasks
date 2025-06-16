namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow
{
    private void PushNode(IFlowNode node)
    {
        _id = node.NodeId;
        _parentId = default;
        _stateMachine = null;
        _suspendingAwaiterOrType = null;
        _node = node;
    }

    private void PopNode()
    {
        IFlowNode node = ConsumeNotNull(ref _node);
        _id = node.Id;
        _parentId = node.ParentId;
        _stateMachine = node.StateMachine;
        _suspendingAwaiterOrType = node.SuspendingAwaiterOrType;
        _node = node.ParentNode;

        if (!node.IsCompleted)
        {
            if (_stateMachine is not null)
            {
                _state = FlowState.Running;
                Suspend(static flow => flow.AwaitOnSuspend());
                return;
            }
            
            _state = FlowState.Suspending;
            Continue();
            return;
        }

        if (_stateMachine is not null)
        {
            _state = FlowState.Running;
            _suspendingAwaiterOrType = null;
            Continue();
            return;
        }
        
        ((IDAsyncRunner)this).Succeed();
    }
    
    private void RunBranch()
    {
        _state = FlowState.Aggregating;
        Continue();
    }
    
    private interface IFlowNode
    {
        DAsyncId Id { get; }
        
        DAsyncId ParentId { get; }
        
        DAsyncId NodeId { get; }
        
        IFlowNode? ParentNode { get; }
        
        IDAsyncStateMachine? StateMachine { get; }
        
        object? SuspendingAwaiterOrType { get; }
        
        bool IsCompleted { get; }
        
        void RunBranch();
        
        void SucceedBranch();
        
        void SucceedBranch<TResult>(TResult result);
        
        void FailBranch(Exception exception);

        void SuspendBranch();
    }
}