using DTasks.Inspection;
using DTasks.Utils;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow
{
    private void SetBranchResult()
    {
        switch (_aggregateType)
        {
            case AggregateType.WhenAll:
                _branchCount--;
                break;

            case AggregateType.WhenAllResult:
                Debug.Fail("Expected a result from completed WhenAll branch.");
                break;

            case AggregateType.WhenAny:
            case AggregateType.WhenAnyResult:
                throw new NotImplementedException();

            default:
                Debug.Fail("Invalid aggregate type.");
                break;
        }
    }

    private void SetBranchResult<TResult>(TResult result)
    {
        switch (_aggregateType)
        {
            case AggregateType.WhenAll:
                _branchCount--;
                break;

            case AggregateType.WhenAllResult:
                Assert.Is<Dictionary<int, TResult>>(_whenAllBranchResults);
                _whenAllBranchResults.Add(_branchCount, result);
                break;

            case AggregateType.WhenAny:
            case AggregateType.WhenAnyResult:
                throw new NotImplementedException();

            default:
                Debug.Fail("Invalid aggregate type.");
                break;
        }
    }

    private void SetBranchException(Exception exception)
    {
        _aggregateExceptions ??= new List<Exception>(1);
        _aggregateExceptions.Add(exception);

        switch (_aggregateType)
        {
            case AggregateType.WhenAll:
            case AggregateType.WhenAllResult:
                _branchCount--;
                break;

            case AggregateType.WhenAny:
            case AggregateType.WhenAnyResult:
                throw new NotImplementedException();

            default:
                Debug.Fail("Invalid aggregate type.");
                break;
        }
    }

    private void SuspendBranch()
    {
        IDAsyncStateMachine? stateMachine = Consume(ref _stateMachine);
        Assert.NotNull(stateMachine);

        _suspendingAwaiterOrType = s_branchSuspensionSentinel;
        _continuation = Continuations.YieldIndirection;
        stateMachine.Suspend();
    }

    private void RunBranchIndexIndirection(FlowContinuation continuation)
    {
        Debug.Assert(_branchIndex != -1);

        WhenAllResultBranchStateMachine branchStateMachine = new()
        {
            BranchIndex = _branchIndex
        };

        DAsyncId parentId = _parentId;
        DAsyncId id = DAsyncId.New();

        _parentId = id;
        _branchIndex = -1;
        _suspendingAwaiterOrType = typeof(WhenAllResultBranchAwaiter);
        _continuation = continuation;
        Dehydrate(parentId, id, ref branchStateMachine);
    }

    private enum AggregateType
    {
        None,
        WhenAll,
        WhenAllResult,
        WhenAny,
        WhenAnyResult
    }

    private abstract class WhenAllResultBranchRunnable : IDAsyncRunnable
    {
        public int BranchIndex;

        public abstract void Run(IDAsyncRunner runner);
    }

    private sealed class WhenAllResultBranchRunnable<TResult>(TResult result) : WhenAllResultBranchRunnable
    {
        public override void Run(IDAsyncRunner runner) => runner.Succeed((BranchIndex, result));
    }

    private struct WhenAllResultBranchRunnableBuilder
    {
        public IDAsyncRunnable Task { get; private set; }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
        {
            // TODO: Inspector should support non-generic start method
            Assert.Is<WhenAllResultBranchStateMachine>(stateMachine);

            WhenAllResultBranchRunnable runnable = Unsafe.As<TStateMachine, WhenAllResultBranchStateMachine>(ref stateMachine).Awaiter.Runnable;
            runnable.BranchIndex = Unsafe.As<TStateMachine, WhenAllResultBranchStateMachine>(ref stateMachine).BranchIndex;
            Task = runnable;
        }

        public static WhenAllResultBranchRunnableBuilder Create() => default;
    }

    private readonly struct WhenAllResultBranchAwaiter(WhenAllResultBranchRunnable runnable)
    {
        public WhenAllResultBranchRunnable Runnable => runnable;

        public static WhenAllResultBranchAwaiter FromResult<TResult>(TResult result) => new(new WhenAllResultBranchRunnable<TResult>(result));

        public static WhenAllResultBranchAwaiter FromException(Exception exception) => throw new NotImplementedException();
    }

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    private struct WhenAllResultBranchStateMachine
    {
        [DAsyncRunnableBuilderField]
        public WhenAllResultBranchRunnableBuilder Builder;

        [DAsyncAwaiterField]
        public WhenAllResultBranchAwaiter Awaiter;

        public int BranchIndex;
    }
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value


    private sealed class WhenAnyRunnable(int branchCount) : IDAsyncRunnable
    {
        public void Run(IDAsyncRunner runner)
        {
            Assert.Is<DAsyncFlow>(runner);
            DAsyncFlow flow = Unsafe.As<DAsyncFlow>(runner);

            WhenAnyStateMachine stateMachine = default;
            //stateMachine.BranchCount = branchCount;
            flow._suspendingAwaiterOrType = typeof(WhenAnyAwaiter);
            flow._continuation = Continuations.Suspend;
            flow.Dehydrate(flow._parentId, flow._id, ref stateMachine);
        }
    }

    private sealed class CompletedWhenAnyRunnable(ref WhenAnyStateMachine stateMachine) : IDAsyncRunnable
    {
        private WhenAnyStateMachine _stateMachine = stateMachine;

        public void Run(IDAsyncRunner runner)
        {
            if (runner is not DAsyncFlow flow)
                throw new ArgumentException("A d-async runnable was resumed on a different flow than the one that started it.");

            if (_stateMachine.IsCompleted)
            {
                flow.Return();
                return;
            }

            _stateMachine.IsCompleted = true;
            flow._suspendingAwaiterOrType = typeof(WhenAnyAwaiter);
            flow._continuation = self =>
            {
                DTask result = self._tasks[self._childId];
                self.Resume(self._parentId, result);
            };
            flow.Dehydrate(flow._parentId, flow._id, ref _stateMachine);
        }
    }

    private struct WhenAnyRunnableBuilder
    {
        public IDAsyncRunnable Task { get; private set; }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
        {
            // TODO: Inspector should support non-generic start method
            Assert.Is<WhenAnyStateMachine>(stateMachine);

            Task = new CompletedWhenAnyRunnable(ref Unsafe.As<TStateMachine, WhenAnyStateMachine>(ref stateMachine));
        }

        public static WhenAnyRunnableBuilder Create() => default;
    }

    private readonly struct WhenAnyAwaiter
    {
        public static WhenAnyAwaiter FromResult() => default;

        public static WhenAnyAwaiter FromResult<TResult>(TResult result) => default;

        public static WhenAnyAwaiter FromException(Exception exception) => throw new NotImplementedException();
    }

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    private struct WhenAnyStateMachine
    {
        [DAsyncRunnableBuilderField]
        public WhenAnyRunnableBuilder Builder;

        [DAsyncAwaiterField]
        public WhenAnyAwaiter Awaiter;

        public bool IsCompleted;
    }
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
}
