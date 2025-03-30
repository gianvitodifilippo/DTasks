using DTasks.Infrastructure;
using DTasks.Marshaling;
using DTasks.Utils;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DTasks.Infrastructure;

internal partial class DAsyncFlow : IDAsyncRunnerInternal
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
        _childId = _id;
        _id = id;

        if (id.IsRoot)
        {
            Succeed();
        }
        else if (IsRunningAggregates && id == _parent._id)
        {
            _branchIndex = -1;
            _parent.SetBranchResult();
            Return();
        }
        else
        {
            Hydrate(id);
        }
    }

    private void Resume<TResult>(DAsyncId id, TResult result)
    {
        _childId = _id;
        _id = id;

        if (id.IsRoot)
        {
            Succeed(result);
        }
        else if (IsRunningAggregates && id == _parent._id)
        {
            _branchIndex = -1;
            _parent.SetBranchResult(result);
            Return();
        }
        else
        {
            Hydrate(id, result);
        }
    }

    private void Resume(DAsyncId id, Exception exception)
    {
        _childId = _id;
        _id = id;

        if (id.IsRoot)
        {
            Fail(exception);
        }
        else if (IsRunningAggregates && id == _parent._id)
        {
            _branchIndex = -1;
            _parent.SetBranchException(exception);
            Return();
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

    private void WhenAll(IEnumerable<IDAsyncRunnable> branches, IDAsyncResultBuilder resultBuilder)
    {
        if (IsRunningAggregates && IsWhenAllResultBranch)
        {
            _aggregateBranches = branches;
            _resultBuilder = resultBuilder;
            RunBranchIndexIndirection(Continuations.WhenAll);
            return;
        }

        Await(WhenAllAsync(branches, resultBuilder), FlowState.Aggregating);
    }

    private async Task WhenAllAsync(IEnumerable<IDAsyncRunnable> branches, IDAsyncResultBuilder resultBuilder)
    {
        Assert.Null(_aggregateExceptions);
        Debug.Assert(_branchCount == 0);

        _aggregateType = AggregateType.WhenAll;
        DAsyncFlow childFlow = new();

        foreach (IDAsyncRunnable branch in branches)
        {
            IDAsyncRunnable runnable = branch is DTask task && _tokens.TryGetValue(task, out DTaskToken? token)
                ? new HandleRunnableWrapper(childFlow, branch, token.Id)
                : branch;

            childFlow._state = FlowState.Running;
            childFlow._parent = this;
            childFlow._host = _host;
            childFlow._marshaler = _marshaler;
            childFlow._stateManager = _stateManager;
            childFlow._parentId = _id;
            childFlow._id = DAsyncId.New();
            childFlow._typeResolver = _typeResolver;

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

            _branchCount++;
        }

        if (_aggregateExceptions is not null)
        {
            if (_branchCount != 0)
                throw new NotImplementedException();

            resultBuilder.SetException(new AggregateException(_aggregateExceptions));
        }

        if (_branchCount == 0)
        {
            resultBuilder.SetResult();
            return;
        }

        int whenAllBranchCount = _branchCount;

        _aggregateType = AggregateType.None;
        _branchCount = 0;
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

    private void WhenAll<TResult>(IEnumerable<IDAsyncRunnable> branches, IDAsyncResultBuilder<TResult[]> resultBuilder)
    {
        if (IsRunningAggregates && IsWhenAllResultBranch)
        {
            _aggregateBranches = branches;
            _resultBuilder = resultBuilder;
            RunBranchIndexIndirection(Continuations.WhenAll<TResult>);
            return;
        }

        Await(WhenAllAsync(branches, resultBuilder), FlowState.Aggregating);
    }

    private async Task WhenAllAsync<TResult>(IEnumerable<IDAsyncRunnable> branches, IDAsyncResultBuilder<TResult[]> resultBuilder)
    {
        Assert.Null(_aggregateExceptions);
        Debug.Assert(_branchCount == 0);

        _aggregateType = AggregateType.WhenAllResult;
        _whenAllBranchResults = new Dictionary<int, TResult>();
        DAsyncFlow childFlow = new();

        foreach (IDAsyncRunnable branch in branches)
        {
            IDAsyncRunnable runnable = branch is DTask task && _tokens.TryGetValue(task, out DTaskToken? token)
                ? new HandleRunnableWrapper(childFlow, branch, token.Id)
                : branch;

            childFlow._state = FlowState.Running;
            childFlow._parent = this;
            childFlow._host = _host;
            childFlow._marshaler = _marshaler;
            childFlow._stateManager = _stateManager;
            childFlow._parentId = _id;
            childFlow._id = DAsyncId.New();
            childFlow._branchIndex = _branchCount;
            childFlow._typeResolver = _typeResolver;

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

            _branchCount++;
        }

        Assert.Is<Dictionary<int, TResult>>(_whenAllBranchResults);
        Dictionary<int, TResult> whenAllBranchResults = Unsafe.As<Dictionary<int, TResult>>(_whenAllBranchResults);
        int branchCount = _branchCount;

        _aggregateType = AggregateType.None;
        _branchCount = 0;
        _whenAllBranchResults = null;
        
        if (_aggregateExceptions is not null)
            throw new NotImplementedException();

        if (branchCount == whenAllBranchResults.Count)
        {
            TResult[] result = ToResultArray(whenAllBranchResults);
            resultBuilder.SetResult(result);
            return;
        }

        _aggregateRunnable = WhenAllDAsync(whenAllBranchResults, branchCount);
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

    private void WhenAny(IEnumerable<IDAsyncRunnable> branches, IDAsyncResultBuilder<DTask> resultBuilder)
    {
        if (IsRunningAggregates && IsWhenAllResultBranch)
        {
            _aggregateBranches = branches;
            _resultBuilder = resultBuilder;
            RunBranchIndexIndirection(Continuations.WhenAll);
            return;
        }

        Await(WhenAnyAsync(branches, resultBuilder), FlowState.Aggregating);
    }

    private async Task WhenAnyAsync(IEnumerable<IDAsyncRunnable> branches, IDAsyncResultBuilder<DTask> resultBuilder)
    {
        Debug.Assert(_branchCount == 0);

        _aggregateType = AggregateType.WhenAny;
        DAsyncFlow childFlow = new();

        foreach (IDAsyncRunnable branch in branches)
        {
            IDAsyncRunnable runnable = branch is DTask task && _tokens.TryGetValue(task, out DTaskToken? token)
                ? new HandleRunnableWrapper(childFlow, branch, token.Id)
                : branch;

            childFlow._state = FlowState.Running;
            childFlow._parent = this;
            childFlow._host = _host;
            childFlow._marshaler = _marshaler;
            childFlow._stateManager = _stateManager;
            childFlow._parentId = _id;
            childFlow._id = DAsyncId.New();
            childFlow._typeResolver = _typeResolver;

            try
            {
                runnable.Run(childFlow);
                await new ValueTask(childFlow, childFlow._valueTaskSource.Version);
            }
            catch
            {
                throw new NotImplementedException();
            }

            _branchCount++;
        }

        int branchCount = _branchCount;

        _aggregateType = AggregateType.None;
        _branchCount = 0;
        _aggregateRunnable = new WhenAnyRunnable(branchCount);
    }

    private void WhenAny<TResult>(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultBuilder<DTask<TResult>> resultBuilder)
    {
        throw new NotImplementedException();
    }

    private void Background(IDAsyncRunnable runnable, IDAsyncResultBuilder<DTask> builder)
    {
        throw new NotImplementedException();
    }

    private void Background<TResult>(IDAsyncRunnable runnable, IDAsyncResultBuilder<DTask<TResult>> builder)
    {
        throw new NotImplementedException();
    }

    private void Handle(DAsyncId id, IDAsyncResultBuilder builder)
    {
        throw new NotImplementedException();
    }

    private void Handle<TResult>(DAsyncId id, IDAsyncResultBuilder<TResult> builder)
    {
        _handleResultType = typeof(TResult);
        _resultBuilder = builder;
        Hydrate(id);
    }

    void IDAsyncRunner.Start(IDAsyncStateMachine stateMachine)
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

    void IDAsyncRunner.Succeed()
    {
        Assert.Null(_continuation);

        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);

        if (currentStateMachine is null)
        {
            Resume(_parentId);
        }
        else
        {
            _continuation = static self => self.Resume(self._parentId);
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncRunner.Succeed<TResult>(TResult result)
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

    void IDAsyncRunner.Fail(Exception exception)
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

    void IDAsyncRunner.Yield()
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

    void IDAsyncRunner.Delay(TimeSpan delay)
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

    void IDAsyncRunnerInternal.Callback(ISuspensionCallback callback)
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

    void IDAsyncRunner.WhenAll(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultBuilder builder)
    {
        Assert.NotNull(runnables);
        Assert.NotNull(builder);
        Assert.Null(_aggregateBranches);
        Assert.Null(_resultBuilder);
        Assert.Null(_continuation);

        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);

        if (currentStateMachine is null)
        {
            WhenAll(runnables, builder);
        }
        else
        {
            _aggregateBranches = runnables;
            _resultBuilder = builder;
            _continuation = Continuations.WhenAll;
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncRunner.WhenAll<TResult>(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultBuilder<TResult[]> builder)
    {
        Assert.NotNull(runnables);
        Assert.NotNull(builder);
        Assert.Null(_aggregateBranches);
        Assert.Null(_resultBuilder);
        Assert.Null(_continuation);

        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);

        if (currentStateMachine is null)
        {
            WhenAll(runnables, builder);
        }
        else
        {
            _aggregateBranches = runnables;
            _resultBuilder = builder;
            _continuation = Continuations.WhenAll<TResult>;
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncRunner.WhenAny(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultBuilder<DTask> builder)
    {
        Assert.NotNull(runnables);
        Assert.NotNull(builder);
        Assert.Null(_aggregateBranches);
        Assert.Null(_resultBuilder);
        Assert.Null(_continuation);

        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);

        if (currentStateMachine is null)
        {
            WhenAny(runnables, builder);
        }
        else
        {
            _aggregateBranches = runnables;
            _resultBuilder = builder;
            _continuation = Continuations.WhenAny;
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncRunner.WhenAny<TResult>(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultBuilder<DTask<TResult>> builder)
    {
        Assert.NotNull(runnables);
        Assert.NotNull(builder);
        Assert.Null(_aggregateBranches);
        Assert.Null(_resultBuilder);
        Assert.Null(_continuation);

        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);

        if (currentStateMachine is null)
        {
            WhenAny(runnables, builder);
        }
        else
        {
            _aggregateBranches = runnables;
            _resultBuilder = builder;
            _continuation = Continuations.WhenAny<TResult>;
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncRunner.Background(IDAsyncRunnable runnable, IDAsyncResultBuilder<DTask> builder)
    {
        Assert.NotNull(runnable);
        Assert.NotNull(builder);
        Assert.Null(_aggregateRunnable);
        Assert.Null(_resultBuilder);
        Assert.Null(_continuation);

        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);

        if (currentStateMachine is null)
        {
            Background(runnable, builder);
        }
        else
        {
            _aggregateRunnable = runnable;
            _resultBuilder = builder;
            _continuation = Continuations.Background;
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncRunner.Background<TResult>(IDAsyncRunnable runnable, IDAsyncResultBuilder<DTask<TResult>> builder)
    {
        Assert.NotNull(runnable);
        Assert.NotNull(builder);
        Assert.Null(_aggregateRunnable);
        Assert.Null(_resultBuilder);
        Assert.Null(_continuation);

        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);

        if (currentStateMachine is null)
        {
            Background(runnable, builder);
        }
        else
        {
            _aggregateRunnable = runnable;
            _resultBuilder = builder;
            _continuation = Continuations.Background<TResult>;
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncRunnerInternal.Handle(DAsyncId id, IDAsyncResultBuilder builder)
    {
        Assert.NotNull(builder);
        Assert.Null(_resultBuilder);
        Assert.Null(_continuation);

        _suspendingAwaiterOrType = null;
        Handle(id, builder);

        //IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);

        //if (currentStateMachine is null)
        //    throw new InvalidOperationException("The provided DTask can be run inside async methods only.");

        //_handleId = id;
        //_resultBuilder = builder;
        //_continuation = Continuations.Handle;
        //currentStateMachine.Suspend(); // TODO: Should not suspend after a WhenAll
    }

    void IDAsyncRunnerInternal.Handle<TResult>(DAsyncId id, IDAsyncResultBuilder<TResult> builder)
    {
        Assert.NotNull(builder);
        Assert.Null(_resultBuilder);
        Assert.Null(_continuation);

        _suspendingAwaiterOrType = null;
        Handle(id, builder);

        //IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);

        //if (currentStateMachine is null)
        //    throw new InvalidOperationException("The provided DTask can be run inside async methods only.");

        //_handleId = id;
        //_resultBuilder = builder;
        //_continuation = Continuations.Handle<TResult>;
        //currentStateMachine.Suspend(); // TODO: Should not suspend after a WhenAll
    }
}
