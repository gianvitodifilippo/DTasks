using System.Diagnostics;
using DTasks.Inspection;
using DTasks.Utils;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow
{
    private void DehydrateWhenAll(ref WhenAllStateMachine stateMachine, DehydrateContinuation continuation)
    {
        Assign(ref _dehydrateContinuation, continuation);
        Assign(ref _suspendingAwaiterOrType, typeof(WhenAllAwaiter));
        
        AwaitDehydrate(ref stateMachine);
    }
    
    private void DehydrateWhenAll<TResult>(ref WhenAllStateMachine<TResult> stateMachine, DehydrateContinuation continuation)
    {
        Assign(ref _dehydrateContinuation, continuation);
        Assign(ref _suspendingAwaiterOrType, typeof(WhenAllAwaiter<TResult>));
        
        AwaitDehydrate(ref stateMachine);
    }

    private static TResult[] GetWhenAllResults<TResult>(Dictionary<int, TResult> results)
    {
        return results
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => kvp.Value)
            .ToArray();
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

        public void SetChildId(DAsyncId id)
        {
            // Don't need it
        }

        public void Succeed()
        {
            ((IDAsyncRunner)flow).Succeed();
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
                if (_exceptions is not null)
                {
                    resultBuilder.SetException(new AggregateException(_exceptions));
                }
                else
                {
                    resultBuilder.SetResult();
                }
                
                flow.PopNode();
                return;
            }

            flow._id = NodeId;
            flow._parentId = Id;

            WhenAllStateMachine stateMachine = default;
            stateMachine.RemainingCount = _remainingCount;
            stateMachine.Exceptions = _exceptions;
            flow.DehydrateWhenAll(ref stateMachine, static flow => flow.PopNode());
        }
    }
    
    private sealed class WhenAllFlowNode<TNodeResult>(
        DAsyncFlow flow,
        IEnumerator<IDAsyncRunnable> branchEnumerator,
        IDAsyncResultBuilder<TNodeResult[]> resultBuilder) : IFlowNode
    {
        private int _remainingCount;
        private int _index = -1;
        private Dictionary<int, TNodeResult>? _results;
        private Dictionary<DAsyncId, int>? _indexes;
        private List<Exception>? _exceptions;
        private TNodeResult[]? _resultArray;
        
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
            _index++;
            branch.Run(flow);
        }
        
        public void SucceedBranch()
        {
            _remainingCount--;
            
            ThrowInvalidResult();
        }

        public void SucceedBranch<TResult>(TResult result)
        {
            _remainingCount--;

            if (result is not TNodeResult nodeResult)
            {
                ThrowInvalidResult();
                return;
            }

            _results ??= new(1);
            _results.Add(_index, nodeResult);
            
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

        public void SetChildId(DAsyncId id)
        {
            _indexes ??= new(1);
            _indexes[id] = _index;
        }

        public void Succeed()
        {
            Debug.Assert(_resultArray is not null);
            ((IDAsyncRunner)flow).Succeed(_resultArray);
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
                if (_exceptions is not null)
                {
                    resultBuilder.SetException(new AggregateException(_exceptions));
                }
                else
                {
                    Assert.NotNull(_results);
                    _resultArray = GetWhenAllResults(_results);
                
                    resultBuilder.SetResult(_resultArray);
                }
                
                flow.PopNode();
                return;
            }

            flow._id = NodeId;
            flow._parentId = Id;
            
            WhenAllStateMachine<TNodeResult> stateMachine = default;
            stateMachine.RemainingCount = _remainingCount;
            stateMachine.Indexes = _indexes;
            stateMachine.Results = _results;
            stateMachine.Exceptions = _exceptions;
            flow.DehydrateWhenAll(ref stateMachine, static flow => flow.PopNode());
        }

        private static void ThrowInvalidResult()
        {
            // TODO: Standardize across project with a dedicated exception type
            throw new InvalidOperationException($"Current DTask should have resumed with a result of type '{typeof(TNodeResult).FullName}'.");
        }
    }

    private abstract class WhenAllBranchRunnable : IDAsyncRunnable
    {
        private int _remainingCount = -1;
        private List<Exception>? _exceptions;
        
        protected abstract void Run(DAsyncFlow flow, ref WhenAllStateMachine stateMachine);
        
        public void Run(IDAsyncRunner runner)
        {
            Debug.Assert(_remainingCount != -1);
            
            if (runner is not DAsyncFlow flow)
                throw new ArgumentException("This runnable should be run by a runner of the same kind than the one that created it.");

            WhenAllStateMachine stateMachine = default;
            stateMachine.RemainingCount = _remainingCount - 1;
            stateMachine.Exceptions = _exceptions;
            Run(flow, ref stateMachine);
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
        protected override void Run(DAsyncFlow flow, ref WhenAllStateMachine stateMachine)
        {
            if (stateMachine.RemainingCount == 0)
            {
                if (stateMachine.Exceptions is { Count: > 0 })
                {
                    ((IDAsyncRunner)flow).Fail(new AggregateException(stateMachine.Exceptions));
                }
                else
                {
                    ((IDAsyncRunner)flow).Succeed();
                }
                return;
            }
            
            flow._frameHasIds = false;
            flow.DehydrateWhenAll(ref stateMachine, static flow => flow.AwaitOnSuspend());
        }
    }

    private sealed class WhenAllFailedBranchRunnable(Exception exception) : WhenAllBranchRunnable
    {
        protected override void Run(DAsyncFlow flow, ref WhenAllStateMachine stateMachine)
        {
            stateMachine.Exceptions ??= new(1);
            stateMachine.Exceptions.Add(exception);

            if (stateMachine.RemainingCount == 0)
            {
                ((IDAsyncRunner)flow).Fail(new AggregateException(stateMachine.Exceptions));
                return;
            }
            
            flow._frameHasIds = false;
            flow.DehydrateWhenAll(ref stateMachine, static flow => flow.AwaitOnSuspend());
        }
    }

    private abstract class WhenAllBranchRunnable<TResult> : IDAsyncRunnable
    {
        private int _remainingCount = -1;
        private Dictionary<DAsyncId, int>? _indexes;
        private Dictionary<int, TResult>? _results;
        private List<Exception>? _exceptions;
        
        protected abstract void Run(DAsyncFlow flow, ref WhenAllStateMachine<TResult> stateMachine);
        
        public void Run(IDAsyncRunner runner)
        {
            Debug.Assert(_remainingCount != -1);
            
            if (runner is not DAsyncFlow flow)
                throw new ArgumentException("This runnable should be run by a runner of the same kind than the one that created it.");

            WhenAllStateMachine<TResult> stateMachine = default;
            stateMachine.RemainingCount = _remainingCount - 1;
            stateMachine.Indexes = _indexes;
            stateMachine.Results = _results;
            stateMachine.Exceptions = _exceptions;
            Run(flow, ref stateMachine);
        }
        
        public WhenAllBranchRunnable<TResult> WithProperties(ref WhenAllStateMachine<TResult> stateMachine)
        {
            _remainingCount = stateMachine.RemainingCount;
            _indexes = stateMachine.Indexes;
            _results = stateMachine.Results;
            _exceptions = stateMachine.Exceptions;
            return this;
        }
    }

    private sealed class WhenAllSucceededBranchRunnable<TResult>(TResult result) : WhenAllBranchRunnable<TResult>
    {
        protected override void Run(DAsyncFlow flow, ref WhenAllStateMachine<TResult> stateMachine)
        {
            Debug.Assert(stateMachine.Indexes is not null);

            bool removeResult = stateMachine.Indexes.Remove(flow._childId, out int index);
            Debug.Assert(removeResult);
            
            stateMachine.Results ??= new(1);
            stateMachine.Results.Add(index, result);
            
            if (stateMachine.RemainingCount == 0)
            {
                if (stateMachine.Exceptions is { Count: > 0 })
                {
                    ((IDAsyncRunner)flow).Fail(new AggregateException(stateMachine.Exceptions));
                }
                else
                {
                    Debug.Assert(stateMachine.Results is not null);
                    
                    TResult[] results = GetWhenAllResults(stateMachine.Results);
                    ((IDAsyncRunner)flow).Succeed(results);
                }
                return;
            }
            
            flow._frameHasIds = false;
            flow.DehydrateWhenAll(ref stateMachine, static flow => flow.AwaitOnSuspend());
        }
    }

    private sealed class WhenAllFailedBranchRunnable<TResult>(Exception exception) : WhenAllBranchRunnable<TResult>
    {
        protected override void Run(DAsyncFlow flow, ref WhenAllStateMachine<TResult> stateMachine)
        {
            Debug.Assert(stateMachine.Indexes is not null);

            bool removeResult = stateMachine.Indexes.Remove(flow._childId, out _);
            Debug.Assert(removeResult);

            stateMachine.Exceptions ??= new(1);
            stateMachine.Exceptions.Add(exception);

            if (stateMachine.RemainingCount == 0)
            {
                ((IDAsyncRunner)flow).Fail(new AggregateException(stateMachine.Exceptions));
                return;
            }
            
            flow._frameHasIds = false;
            flow.DehydrateWhenAll(ref stateMachine, static flow => flow.AwaitOnSuspend());
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
    
    private struct WhenAllRunnableBuilder<TResult>
    {
        public IDAsyncRunnable Task { get; private set; }

        public void Start(ref WhenAllStateMachine<TResult> stateMachine)
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

    private readonly struct WhenAllAwaiter<TNodeResult>(WhenAllBranchRunnable<TNodeResult> runnable)
    {
        public readonly WhenAllBranchRunnable<TNodeResult> Runnable = runnable;

        public static WhenAllAwaiter<TNodeResult> FromResult() => throw InvalidResult();

        public static WhenAllAwaiter<TNodeResult> FromResult<TResult>(TResult result) => result is TNodeResult nodeResult
            ? new(new WhenAllSucceededBranchRunnable<TNodeResult>(nodeResult))
            : throw InvalidResult();

        public static WhenAllAwaiter<TNodeResult> FromException(Exception exception) => new(new WhenAllFailedBranchRunnable<TNodeResult>(exception));

        private static InvalidOperationException InvalidResult()
        {
            // TODO: Standardize across project with a dedicated exception type
            throw new($"Current DTask should have resumed with a result of type '{typeof(TNodeResult).FullName}'.");
        }
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
    
    private struct WhenAllStateMachine<TResult>
    {
        [DAsyncRunnableBuilderField]
        public WhenAllRunnableBuilder<TResult> Builder;

        [DAsyncAwaiterField]
        public WhenAllAwaiter<TResult> Awaiter;

        public int RemainingCount;

        public Dictionary<DAsyncId, int>? Indexes;

        public Dictionary<int, TResult>? Results;
        
        public List<Exception>? Exceptions;
    }
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
}