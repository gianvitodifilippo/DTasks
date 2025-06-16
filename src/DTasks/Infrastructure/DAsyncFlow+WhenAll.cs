using System.Diagnostics;
using DTasks.Inspection;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow
{
    private sealed class WhenAllFlowNode(
        DAsyncFlow flow,
        IEnumerator<IDAsyncRunnable> branchEnumerator,
        IDAsyncResultBuilder resultBuilder) : IFlowNode
    {
        private int _remainingCount;
        private List<Exception>? _exceptions;
        
        public DAsyncId Id { get; } = flow._id;

        public DAsyncId ParentId { get; } = flow._parentId;

        public DAsyncId NodeId { get; } = flow._idFactory.NewId();

        public IFlowNode? ParentNode { get; } = flow._node;

        public IDAsyncStateMachine? StateMachine { get; } = flow._stateMachine;

        public object? SuspendingAwaiterOrType { get; } = flow._suspendingAwaiterOrType;
        
        public bool IsCompleted => _remainingCount == 0;

        public void RunBranch()
        {
            IDAsyncRunnable branch = branchEnumerator.Current;
            _remainingCount++;
            branch.Run(flow);
        }
        
        public void SucceedBranch()
        {
            _remainingCount--;

            MoveNext();
        }

        public void SucceedBranch<TResult>(TResult result)
        {
            _remainingCount--;

            MoveNext();
        }

        public void FailBranch(Exception exception)
        {
            _remainingCount--;
            _exceptions ??= new(1);
            _exceptions.Add(exception);

            MoveNext();
        }

        public void SuspendBranch()
        {
            MoveNext();
        }

        private void MoveNext()
        {
            if (branchEnumerator.MoveNext())
            {
                flow.RunBranch();
                return;
            }
            
            branchEnumerator.Dispose();
                
            if (_remainingCount == 0)
            {
                resultBuilder.SetResult();
                flow.PopNode();
                return;
            }

            Assign(ref flow._dehydrateContinuation, static flow => flow.PopNode());
            Assign(ref flow._suspendingAwaiterOrType, typeof(WhenAllAwaiter));
            flow._id = NodeId;
            flow._parentId = Id;
            
            WhenAllStateMachine stateMachine = default;
            stateMachine.RemainingCount = _remainingCount;
            stateMachine.Exceptions = _exceptions;
            
            flow.AwaitDehydrate(ref stateMachine);
        }
    }

    private abstract class WhenAllBranchRunnable : IDAsyncRunnable
    {
        private int _remainingCount = -1;
        private List<Exception>? _exceptions;
        
        public void Run(IDAsyncRunner runner)
        {
            Debug.Assert(_remainingCount != -1);
            
            if (runner is not DAsyncFlow flow)
                throw new ArgumentException("This runnable should be run by a runner of the same kind than the one that created it.");

            throw new NotImplementedException();
            // Assign(ref flow._whenAllRemainingCount, _remainingCount - 1);
            // Assign(ref flow._whenAllExceptions, _exceptions);
            // HandleResult(flow);
            //
            // flow.DehydrateWhenAll();
        }
        
        public WhenAllBranchRunnable WithProperties(ref WhenAllStateMachine stateMachine)
        {
            _remainingCount = stateMachine.RemainingCount;
            _exceptions = stateMachine.Exceptions;
            return this;
        }
    }

    private sealed class WhenAllSucceededBranchRunnable : WhenAllBranchRunnable
    {
    }

    private sealed class WhenAllFailedBranchRunnable(Exception exception) : WhenAllBranchRunnable
    {
    }
    
    private struct WhenAllRunnableBuilder
    {
        public IDAsyncRunnable Task { get; private set; }

        public void Start(ref WhenAllStateMachine stateMachine)
        {
            Task = stateMachine.Awaiter.Runnable.WithProperties(ref stateMachine);
        }

        public static WhenAllRunnableBuilder Create() => default;
    }

    private readonly struct WhenAllAwaiter(WhenAllBranchRunnable runnable)
    {
        public readonly WhenAllBranchRunnable Runnable = runnable;
        
        public static WhenAllAwaiter FromResult() => new(new WhenAllSucceededBranchRunnable());

        public static WhenAllAwaiter FromResult<TResult>(TResult result) => new(new WhenAllSucceededBranchRunnable());

        public static WhenAllAwaiter FromException(Exception exception) => new(new WhenAllFailedBranchRunnable(exception));
    }

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    private struct WhenAllStateMachine
    {
        [DAsyncRunnableBuilderField]
        public WhenAllRunnableBuilder Builder;

        [DAsyncAwaiterField]
        public WhenAllAwaiter Awaiter;

        public int RemainingCount;
        
        public List<Exception>? Exceptions;
    }
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
}