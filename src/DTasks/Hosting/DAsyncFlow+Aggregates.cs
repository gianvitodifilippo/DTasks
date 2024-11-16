using DTasks.Inspection;
using DTasks.Utils;
using System.Diagnostics;

namespace DTasks.Hosting;

internal partial class DAsyncFlow
{
    private void SetBranchResult()
    {
        switch (_aggregateType)
        {
            case AggregateType.WhenAll:
                _whenAllBranchCount--;
                break;

            case AggregateType.WhenAllResult:
                Debug.Fail("Expected a result from completed WhenAll branch.");
                break;

            default:
                Debug.Fail("Invalid aggregate state.");
                break;
        }
    }

    private void SetBranchResult<TResult>(TResult result)
    {
        switch (_aggregateType)
        {
            case AggregateType.WhenAll:
                _whenAllBranchCount--;
                break;

            case AggregateType.WhenAllResult:
                Assert.Is<Dictionary<int, TResult>>(_whenAllBranchResults);
                _whenAllBranchResults.Add(_whenAllBranchCount, result);
                break;

            default:
                Debug.Fail("Invalid aggregate state.");
                break;
        }
    }

    private void SetBranchException(Exception exception)
    {
        _aggregateExceptions ??= new(1);
        _aggregateExceptions.Add(exception);

        switch (_aggregateType)
        {
            case AggregateType.WhenAll:
            case AggregateType.WhenAllResult:
                _whenAllBranchCount--;
                break;

            default:
                Debug.Fail("Invalid aggregate state.");
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
        WhenAllResult
    }

    private abstract class WhenAllResultBranchRunnable : IDAsyncRunnable
    {
        public int BranchIndex;

        public abstract void Run(IDAsyncFlow flow);
    }

    private sealed class WhenAllResultBranchRunnable<TResult>(TResult result) : WhenAllResultBranchRunnable
    {
        public override void Run(IDAsyncFlow flow) => flow.Succeed((BranchIndex, result));
    }

    private struct WhenAllResultBranchRunnableBuilder
    {
        public IDAsyncRunnable Task { get; private set; }

        public void Start(ref WhenAllResultBranchStateMachine stateMachine)
        {
            WhenAllResultBranchRunnable runnable = stateMachine.Awaiter.Runnable;
            runnable.BranchIndex = stateMachine.BranchIndex;
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
}
