﻿using DTasks.CompilerServices;
using DTasks.Hosting;
using DTasks.Utils;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DTasks;

public readonly struct BackgroundDAwaitable(IDAsyncRunnable runnable)
{
    private readonly BackgroundDTask<DTask> _backgroundTask = new(runnable);

    public Awaiter GetAwaiter() => new(_backgroundTask);

    public readonly struct Awaiter : ICriticalNotifyCompletion, IDAsyncAwaiter
    {
        private readonly BackgroundDTask<DTask> _backgroundTask;

        internal Awaiter(BackgroundDTask<DTask> backgroundTask)
        {
            _backgroundTask = backgroundTask;
        }

        public bool IsCompleted => false;

        bool IDAsyncAwaiter.IsCompleted => false;

        public DTask GetResult() => _backgroundTask.GetAwaiter().GetResult();

        public void UnsafeOnCompleted(Action continuation) => _backgroundTask.GetAwaiter().UnsafeOnCompleted(continuation);

        public void OnCompleted(Action continuation) => _backgroundTask.GetAwaiter().OnCompleted(continuation);

        public void Continue(IDAsyncFlow flow) => flow.Background(_backgroundTask.Runnable, _backgroundTask);
    }
}

public readonly struct BackgroundDAwaitable<TResult>(IDAsyncRunnable runnable)
{
    private readonly BackgroundDTask<DTask<TResult>> _backgroundTask = new(runnable);

    public Awaiter GetAwaiter() => new(_backgroundTask);

    public readonly struct Awaiter : ICriticalNotifyCompletion, IDAsyncAwaiter
    {
        private readonly BackgroundDTask<DTask<TResult>> _backgroundTask;

        internal Awaiter(BackgroundDTask<DTask<TResult>> backgroundTask)
        {
            _backgroundTask = backgroundTask;
        }

        public bool IsCompleted => false;

        bool IDAsyncAwaiter.IsCompleted => false;

        public DTask GetResult() => _backgroundTask.GetAwaiter().GetResult();

        public void UnsafeOnCompleted(Action continuation) => _backgroundTask.GetAwaiter().UnsafeOnCompleted(continuation);

        public void OnCompleted(Action continuation) => _backgroundTask.GetAwaiter().OnCompleted(continuation);

        public void Continue(IDAsyncFlow flow) => flow.Background(_backgroundTask.Runnable, _backgroundTask);
    }
}

internal sealed class BackgroundDTask<TTask>(IDAsyncRunnable runnable) : DTask<TTask>, IDAsyncResultCallback<TTask>
    where TTask : DTask
{
    private DTaskStatus _status = DTaskStatus.Pending;
    private object _stateObject = runnable;

    public override DTaskStatus Status => _status;

    protected override TTask ResultCore
    {
        get
        {
            Assert.Is<TTask>(_stateObject);

            return Unsafe.As<TTask>(_stateObject);
        }
    }

    protected override Exception ExceptionCore
    {
        get
        {
            Assert.Is<Exception>(_stateObject);

            return Unsafe.As<Exception>(_stateObject);
        }
    }

    public IDAsyncRunnable Runnable
    {
        get
        {
            Assert.Is<IDAsyncRunnable>(_stateObject);

            return Unsafe.As<IDAsyncRunnable>(_stateObject);
        }
    }

    protected override void Run(IDAsyncFlow flow)
    {
        Debug.Fail($"'{nameof(Run)}' should not be invoked on {nameof(BackgroundDTask<DTask>)}.");
    }

    void IDAsyncResultCallback<TTask>.SetResult(TTask result)
    {
        _status = DTaskStatus.Succeeded;
        _stateObject = result;
    }

    void IDAsyncResultCallback<TTask>.SetException(Exception exception)
    {
        _status = DTaskStatus.Faulted;
        _stateObject = exception;
    }
}
