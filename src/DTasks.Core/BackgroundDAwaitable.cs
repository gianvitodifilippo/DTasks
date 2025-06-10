using System.Diagnostics;
using System.Runtime.CompilerServices;
using DTasks.Infrastructure;
using DTasks.Utils;

namespace DTasks;

public readonly ref struct BackgroundDAwaitable(IDAsyncRunnable runnable)
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

        void IDAsyncAwaiter.Continue(IDAsyncRunner runner) => runner.Background(_backgroundTask.Runnable, _backgroundTask);
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

        public DTask<TResult> GetResult() => _backgroundTask.GetAwaiter().GetResult();

        public void UnsafeOnCompleted(Action continuation) => _backgroundTask.GetAwaiter().UnsafeOnCompleted(continuation);

        public void OnCompleted(Action continuation) => _backgroundTask.GetAwaiter().OnCompleted(continuation);

        void IDAsyncAwaiter.Continue(IDAsyncRunner runner) => runner.Background(_backgroundTask.Runnable, _backgroundTask);
    }
}

internal sealed class BackgroundDTask<TTask>(IDAsyncRunnable runnable) : DTask<TTask>, IDAsyncResultBuilder<TTask>
    where TTask : DTask
{
    private DTaskStatus _status = DTaskStatus.Pending;
    private object _stateObject = runnable;

    public override DTaskStatus Status => _status;

    protected override TTask ResultCore => Reinterpret.Cast<TTask>(_stateObject);

    protected override Exception ExceptionCore => Reinterpret.Cast<Exception>(_stateObject);

    public IDAsyncRunnable Runnable => Reinterpret.Cast<IDAsyncRunnable>(_stateObject);

    protected override void Run(IDAsyncRunner runner)
    {
        Debug.Fail($"'{nameof(Run)}' should not be invoked on {nameof(BackgroundDTask<DTask>)}.");
    }

    void IDAsyncResultBuilder<TTask>.SetResult(TTask result)
    {
        _status = DTaskStatus.Succeeded;
        _stateObject = result;
    }

    void IDAsyncResultBuilder<TTask>.SetException(Exception exception)
    {
        _status = DTaskStatus.Faulted;
        _stateObject = exception;
    }
}
