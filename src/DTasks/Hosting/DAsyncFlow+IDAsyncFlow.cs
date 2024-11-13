using DTasks.Utils;
using System.Runtime.CompilerServices;

namespace DTasks.Hosting;

internal partial class DAsyncFlow : IDAsyncFlowInternal
{
    private static readonly object s_branchSuspensionSentinel = new();

    private void Start()
    {
        Assert.NotNull(_stateMachine);

        _stateMachine.Start(this);

        if (IsRunningAggregates)
        {
            if (IsWhenAllResultBranch)
            {
                RunBranchIndexIndirection(Continuations.SuspendBranch);
            }
            else
            {
                SuspendBranch();
            }
            return;
        }

        _stateMachine.MoveNext();
    }

    private void Resume(DAsyncId id)
    {
        _id = id;

        if (id.IsRoot)
        {
            Succeed();
        }
        else if (IsRunningAggregates && id == _parent._id)
        {
            _branchIndex = -1;
            _parent.SetBranchResult();
            Await(Task.CompletedTask, FlowState.Returning);
        }
        else
        {
            Hydrate(id);
        }
    }

    private void Resume<TResult>(DAsyncId id, TResult result)
    {
        _id = id;

        if (id.IsRoot)
        {
            Succeed(result);
        }
        else if (IsRunningAggregates && id == _parent._id)
        {
            _branchIndex = -1;
            _parent.SetBranchResult(result);
            Await(Task.CompletedTask, FlowState.Returning);
        }
        else
        {
            Hydrate(id, result);
        }
    }

    private void Resume(DAsyncId id, Exception exception)
    {
        _id = id;

        if (id.IsRoot)
        {
            Fail(exception);
        }
        else if (IsRunningAggregates && id == _parent._id)
        {
            _branchIndex = -1;
            _parent.SetBranchException(exception);
            Await(Task.CompletedTask, FlowState.Returning);
        }
        else
        {
            Hydrate(id, exception);
        }
    }

    private void Yield()
    {
        if (IsRunningAggregates && IsWhenAllResultBranch)
        {
            RunBranchIndexIndirection(Continuations.Yield);
            return;
        }

        _suspendingAwaiterOrType = null;
        Await(_host.YieldAsync(_id, _cancellationToken), FlowState.Returning);
    }

    private void Delay(TimeSpan delay)
    {
        if (IsRunningAggregates && IsWhenAllResultBranch)
        {
            _delay = delay;
            RunBranchIndexIndirection(Continuations.Delay);
            return;
        }

        _suspendingAwaiterOrType = null;
        Await(_host.DelayAsync(_id, delay, _cancellationToken), FlowState.Returning);
    }

    private void Callback(ISuspensionCallback callback)
    {
        if (IsRunningAggregates && IsWhenAllResultBranch)
        {
            _callback = callback;
            RunBranchIndexIndirection(Continuations.Callback);
            return;
        }

        _suspendingAwaiterOrType = null;
        Await(callback.InvokeAsync(_id, _cancellationToken), FlowState.Returning);
    }

    private void WhenAll(IEnumerable<IDAsyncRunnable> aggregateBranches, IDAsyncResultCallback resultCallback)
    {
        if (IsRunningAggregates && IsWhenAllResultBranch)
        {
            _aggregateBranches = aggregateBranches;
            _resultCallback = resultCallback;
            RunBranchIndexIndirection(Continuations.WhenAll);
            return;
        }

        Await(WhenAllAsync(aggregateBranches, resultCallback), FlowState.Aggregating);
    }

    private async Task WhenAllAsync(IEnumerable<IDAsyncRunnable> aggregateBranches, IDAsyncResultCallback resultCallback)
    {
        Assert.Null(_aggregateExceptions);

        _aggregateType = AggregateType.WhenAll;
        DAsyncFlow childFlow = new();

        foreach (IDAsyncRunnable runnable in aggregateBranches)
        {
            DAsyncId id = DAsyncId.New();

            childFlow._state = FlowState.Running;
            childFlow._parent = this;
            childFlow._host = _host;
            childFlow._marshaler = _marshaler;
            childFlow._stateManager = _stateManager;
            childFlow._parentId = _id;
            childFlow._id = id;
            // childFlow._typeResolver = _typeResolver;

            try
            {
                runnable.Run(childFlow);
                await new ValueTask(childFlow, childFlow._valueTaskSource.Version);
            }
            catch (Exception ex)
            {
                _aggregateExceptions ??= new(1);
                _aggregateExceptions.Add(ex);
            }

            _whenAllBranchCount++;
        }

        if (_aggregateExceptions is not null)
        {
            if (_whenAllBranchCount != 0)
                throw new NotImplementedException();

            resultCallback.SetException(new AggregateException(_aggregateExceptions));
        }

        if (_whenAllBranchCount == 0)
        {
            resultCallback.SetResult();
            return;
        }

        int whenAllBranchCount = _whenAllBranchCount;

        _aggregateType = AggregateType.None;
        _whenAllBranchCount = 0;
        _aggregateRunnable = WhenAllDAsync(whenAllBranchCount);
    }

    private static async DTask WhenAllDAsync(int branchCount)
    {
        while (branchCount > 0)
        {
            await SuspendedDTask.Instance;
            branchCount--;
        }
    }

    private void WhenAll<TResult>(IEnumerable<IDAsyncRunnable> aggregateBranches, IDAsyncResultCallback<TResult[]> resultCallback)
    {
        if (IsRunningAggregates && IsWhenAllResultBranch)
        {
            _aggregateBranches = aggregateBranches;
            _resultCallback = resultCallback;
            RunBranchIndexIndirection(Continuations.WhenAll<TResult>);
            return;
        }

        Await(WhenAllAsync(aggregateBranches, resultCallback), FlowState.Aggregating);
    }

    private async Task WhenAllAsync<TResult>(IEnumerable<IDAsyncRunnable> aggregateBranches, IDAsyncResultCallback<TResult[]> resultCallback)
    {
        Assert.Null(_aggregateExceptions);

        _aggregateType = AggregateType.WhenAllResult;
        _whenAllBranchResults = new Dictionary<int, TResult>();
        DAsyncFlow childFlow = new();

        foreach (IDAsyncRunnable runnable in aggregateBranches)
        {
            DAsyncId id = DAsyncId.New();

            childFlow._state = FlowState.Running;
            childFlow._parent = this;
            childFlow._host = _host;
            childFlow._marshaler = _marshaler;
            childFlow._stateManager = _stateManager;
            childFlow._parentId = _id;
            childFlow._id = id;
            childFlow._branchIndex = _whenAllBranchCount;
            // childFlow._typeResolver = _typeResolver;

            try
            {
                runnable.Run(childFlow);
                await new ValueTask(childFlow, childFlow._valueTaskSource.Version);
            }
            catch (Exception ex)
            {
                _aggregateExceptions ??= new(1);
                _aggregateExceptions.Add(ex);
            }

            _whenAllBranchCount++;
        }

        Assert.Is<Dictionary<int, TResult>>(_whenAllBranchResults);
        Dictionary<int, TResult> whenAllBranchResults = Unsafe.As<Dictionary<int, TResult>>(_whenAllBranchResults);
        int whenAllBranchCount = _whenAllBranchCount;

        _aggregateType = AggregateType.None;
        _whenAllBranchCount = 0;
        _whenAllBranchResults = null;
        
        if (_aggregateExceptions is not null)
            throw new NotImplementedException();

        if (_whenAllBranchCount == whenAllBranchResults.Count)
        {
            TResult[] result = ToResultArray(whenAllBranchResults);
            resultCallback.SetResult(result);
            return;
        }

        _aggregateRunnable = WhenAllDAsync(whenAllBranchResults, whenAllBranchCount);
    }

    private static async DTask<TResult[]> WhenAllDAsync<TResult>(Dictionary<int, TResult> branchResults, int branchCount)
    {
        while (branchResults.Count < branchCount)
        {
            (int branchIndex, TResult result) = await SuspendedDTask<(int, TResult)>.Instance;
            branchResults.Add(branchIndex, result);
        }

        return ToResultArray(branchResults);
    }

    private static TResult[] ToResultArray<TResult>(Dictionary<int, TResult> branchResults)
    {
        TResult[] results = new TResult[branchResults.Count];
        foreach (KeyValuePair<int, TResult> branchResult in branchResults)
        {
            results[branchResult.Key] = branchResult.Value;
        }

        return results;
    }

    private void WhenAny(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultCallback<DTask> callback)
    {
        throw new NotImplementedException();
    }

    private void WhenAny<TResult>(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultCallback<DTask<TResult>> callback)
    {
        throw new NotImplementedException();
    }

    private void Background(IDAsyncRunnable runnable, IDAsyncResultCallback<DTask> callback)
    {
        throw new NotImplementedException();
    }

    private void Background<TResult>(IDAsyncRunnable runnable, IDAsyncResultCallback<DTask<TResult>> callback)
    {
        throw new NotImplementedException();
    }

    void IDAsyncFlow.Start(IDAsyncStateMachine stateMachine)
    {
        Assert.NotNull(stateMachine);
        Assert.Null(_continuation);

        IDAsyncStateMachine? currentStateMachine = _stateMachine;
        _stateMachine = stateMachine;

        if (currentStateMachine is null)
        {
            Start();
        }
        else
        {
            _continuation = Continuations.Start;
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncFlow.Succeed()
    {
        Assert.Null(_continuation);

        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);

        if (currentStateMachine is null)
        {
            Resume(_parentId);
        }
        else
        {
            _continuation = self => self.Resume(self._parentId);
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncFlow.Succeed<TResult>(TResult result)
    {
        Assert.Null(_continuation);

        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);

        if (currentStateMachine is null)
        {
            Resume(_parentId, result);
        }
        else
        {
            _continuation = self => self.Resume(self._parentId, result);
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncFlow.Fail(Exception exception)
    {
        Assert.NotNull(exception);
        Assert.Null(_continuation);

        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);

        if (currentStateMachine is null)
        {
            Resume(_parentId, exception);
        }
        else
        {
            _continuation = self => self.Resume(self._parentId, exception);
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncFlow.Yield()
    {
        Assert.Null(_continuation);

        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);

        if (currentStateMachine is null)
        {
            RunIndirection(Continuations.Yield);
        }
        else
        {
            _continuation = Continuations.YieldIndirection;
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncFlow.Delay(TimeSpan delay)
    {
        Assert.Null(_continuation);

        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);
        _delay = delay;

        if (currentStateMachine is null)
        {
            RunIndirection(Continuations.Delay);
        }
        else
        {
            _continuation = Continuations.DelayIndirection;
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncFlowInternal.Callback(ISuspensionCallback callback)
    {
        Assert.NotNull(callback);
        Assert.Null(_continuation);
        Assert.Null(_callback);

        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);
        _callback = callback;

        if (currentStateMachine is null)
        {
            RunIndirection(Continuations.Callback);
        }
        else
        {
            _continuation = Continuations.CallbackIndirection;
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncFlow.WhenAll(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultCallback callback)
    {
        Assert.NotNull(runnables);
        Assert.NotNull(callback);
        Assert.Null(_aggregateBranches);
        Assert.Null(_resultCallback);
        Assert.Null(_continuation);

        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);

        if (currentStateMachine is null)
        {
            WhenAll(runnables, callback);
        }
        else
        {
            _aggregateBranches = runnables;
            _resultCallback = callback;
            _continuation = Continuations.WhenAll;
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncFlow.WhenAll<TResult>(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultCallback<TResult[]> callback)
    {
        Assert.NotNull(runnables);
        Assert.NotNull(callback);
        Assert.Null(_aggregateBranches);
        Assert.Null(_resultCallback);
        Assert.Null(_continuation);

        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);

        if (currentStateMachine is null)
        {
            WhenAll(runnables, callback);
        }
        else
        {
            _aggregateBranches = runnables;
            _resultCallback = callback;
            _continuation = Continuations.WhenAll<TResult>;
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncFlow.WhenAny(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultCallback<DTask> callback)
    {
        Assert.NotNull(runnables);
        Assert.NotNull(callback);
        Assert.Null(_aggregateBranches);
        Assert.Null(_resultCallback);
        Assert.Null(_continuation);

        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);

        if (currentStateMachine is null)
        {
            WhenAny(runnables, callback);
        }
        else
        {
            _aggregateBranches = runnables;
            _resultCallback = callback;
            _continuation = Continuations.WhenAny;
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncFlow.WhenAny<TResult>(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultCallback<DTask<TResult>> callback)
    {
        Assert.NotNull(runnables);
        Assert.NotNull(callback);
        Assert.Null(_aggregateBranches);
        Assert.Null(_resultCallback);
        Assert.Null(_continuation);

        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);

        if (currentStateMachine is null)
        {
            WhenAny(runnables, callback);
        }
        else
        {
            _aggregateBranches = runnables;
            _resultCallback = callback;
            _continuation = Continuations.WhenAny<TResult>;
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncFlow.Background(IDAsyncRunnable runnable, IDAsyncResultCallback<DTask> callback)
    {
        Assert.NotNull(runnable);
        Assert.NotNull(callback);
        Assert.Null(_aggregateRunnable);
        Assert.Null(_resultCallback);
        Assert.Null(_continuation);

        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);

        if (currentStateMachine is null)
        {
            Background(runnable, callback);
        }
        else
        {
            _aggregateRunnable = runnable;
            _resultCallback = callback;
            _continuation = Continuations.Background;
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncFlow.Background<TResult>(IDAsyncRunnable runnable, IDAsyncResultCallback<DTask<TResult>> callback)
    {
        Assert.NotNull(runnable);
        Assert.NotNull(callback);
        Assert.Null(_aggregateRunnable);
        Assert.Null(_resultCallback);
        Assert.Null(_continuation);

        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);

        if (currentStateMachine is null)
        {
            Background(runnable, callback);
        }
        else
        {
            _aggregateRunnable = runnable;
            _resultCallback = callback;
            _continuation = Continuations.Background<TResult>;
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncFlowInternal.Handle(DAsyncId id)
    {
        throw new NotImplementedException();
    }

    void IDAsyncFlowInternal.Handle<TResult>(DAsyncId id)
    {
        throw new NotImplementedException();
    }
}
