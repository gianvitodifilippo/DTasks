using System.Diagnostics;
using DTasks.Inspection;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow
{
    private void DehydrateWhenAll(int remainingCount, List<Exception>? exceptions, DehydrateContinuation continuation)
    {
        Assign(ref _dehydrateContinuation, continuation);
        Assign(ref _suspendingAwaiterOrType, typeof(WhenAllAwaiter));
        WhenAllStateMachine stateMachine = default;
        stateMachine.RemainingCount = remainingCount;
        stateMachine.Exceptions = exceptions;
            
        AwaitDehydrate(ref stateMachine);
    }
    
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

            flow._id = NodeId;
            flow._parentId = Id;
            flow.DehydrateWhenAll(_remainingCount, _exceptions, static flow => flow.PopNode());
        }
    }

    private abstract class WhenAllBranchRunnable : IDAsyncRunnable
    {
        private int _remainingCount = -1;
        private List<Exception>? _exceptions;
        
        protected abstract void Run(DAsyncFlow flow, int remainingCount, List<Exception>? exceptions);
        
        public void Run(IDAsyncRunner runner)
        {
            Debug.Assert(_remainingCount != -1);
            
            if (runner is not DAsyncFlow flow)
                throw new ArgumentException("This runnable should be run by a runner of the same kind than the one that created it.");

            Run(flow, _remainingCount - 1, _exceptions);
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
        protected override void Run(DAsyncFlow flow, int remainingCount, List<Exception>? exceptions)
        {
            if (remainingCount == 0)
            {
                ((IDAsyncRunner)flow).Succeed();
                return;
            }
            
            flow._frameHasIds = false;
            flow.DehydrateWhenAll(remainingCount, exceptions, static flow => flow.AwaitOnSuspend());
        }
    }

    private sealed class WhenAllFailedBranchRunnable(Exception exception) : WhenAllBranchRunnable
    {
        protected override void Run(DAsyncFlow flow, int remainingCount, List<Exception>? exceptions)
        {
            exceptions ??= new(1);
            exceptions.Add(exception);

            if (remainingCount == 0)
            {
                ((IDAsyncRunner)flow).Fail(new AggregateException(exceptions));
                return;
            }
            
            flow._frameHasIds = false;
            flow.DehydrateWhenAll(remainingCount, exceptions, static flow => flow.AwaitOnSuspend());
        }
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