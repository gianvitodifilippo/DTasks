using DTasks.Host;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DTasks;

[AsyncMethodBuilder(typeof(AsyncDTaskMethodBuilder))]
public abstract class DTask
{
    private protected abstract Task<bool> UnderlyingTask { get; }

    internal virtual Type AwaiterType => typeof(Awaiter);

    public abstract DTaskStatus Status { get; }

    public bool IsCompleted => Status is DTaskStatus.RanToCompletion; // TODO: or faulted or canceled

    public bool IsSuspended => Status is DTaskStatus.Suspended;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public Awaiter GetAwaiter() => new Awaiter(this);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public DAwaiter GetDAwaiter() => new DAwaiter(this);

    internal abstract Task OnSuspendedAsync<THandler>(ref THandler handler, CancellationToken cancellationToken)
        where THandler : ISuspensionHandler;

    public static DTask CompletedTask { get; } = new CompletedDTask<VoidDTaskResult>(default);

    public static DTaskFactory Factory => DTaskFactory.Instance;

    public static DTask<TResult> FromResult<TResult>(TResult result) => new CompletedDTask<TResult>(result);

    public static DTask Yield() => YieldDTask.Instance;

    public static DTask Delay(TimeSpan delay) => new DelayDTask(delay);

    private protected void EnsureCompleted()
    {
        if (!IsCompleted)
            throw new InvalidOperationException("The DTask was not completed.");
    }

    private protected void EnsureSuspended()
    {
        if (!IsSuspended)
            throw new InvalidOperationException("The DTask was not suspended.");
    }

    [Conditional("DEBUG")]
    private protected void VerifyStatus(DTaskStatus expectedStatus)
    {
        Debug.Assert(Status == expectedStatus, $"The DTask was not '{expectedStatus}'.");
    }

    [Conditional("DEBUG")]
    private protected static void InvalidStatus(DTaskStatus expectedStatus)
    {
        Debug.Fail($"The DTask was not '{expectedStatus}'.");
    }

    public readonly struct Awaiter : ICriticalNotifyCompletion, IDTaskAwaiter
    {
        private readonly DTask _task;

        internal Awaiter(DTask task)
        {
            _task = task;
        }

        DTask IDTaskAwaiter.Task => _task;

        private TaskAwaiter<bool> UnderlyingTaskAwaiter => _task.UnderlyingTask.GetAwaiter();

        public bool IsCompleted => _task.IsCompleted;

        public void GetResult()
        {
            _task.EnsureCompleted();
        }

        public void OnCompleted(Action continuation) => UnderlyingTaskAwaiter.OnCompleted(continuation);

        public void UnsafeOnCompleted(Action continuation) => UnderlyingTaskAwaiter.UnsafeOnCompleted(continuation);
    }

    public readonly struct DAwaiter
    {
        private readonly DTask _task;

        internal DAwaiter(DTask task)
        {
            _task = task;
        }

        public Task<bool> IsCompletedAsync()
        {
            return _task.UnderlyingTask;
        }

        public void GetResult()
        {
            _task.EnsureCompleted();
        }

        public Task OnCompletedAsync<THandler>(ref THandler handler, CancellationToken cancellationToken = default)
            where THandler : ICompletionHandler
        {
            _task.EnsureCompleted();
            return handler.OnCompletedAsync(cancellationToken);
        }

        public Task OnSuspendedAsync<THandler>(ref THandler handler, CancellationToken cancellationToken = default)
            where THandler : ISuspensionHandler
        {
            _task.EnsureSuspended();
            return _task.OnSuspendedAsync(ref handler, cancellationToken);
        }
    }
}

[AsyncMethodBuilder(typeof(AsyncDTaskMethodBuilder<>))]
public abstract class DTask<TResult> : DTask
{
    internal abstract TResult Result { get; }

    internal sealed override Type AwaiterType => typeof(Awaiter);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public new Awaiter GetAwaiter() => new Awaiter(this);

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public new DAwaiter GetDAwaiter() => new DAwaiter(this);

    public new readonly struct Awaiter : ICriticalNotifyCompletion, IDTaskAwaiter
    {
        private readonly DTask<TResult> _task;

        internal Awaiter(DTask<TResult> task)
        {
            _task = task;
        }

        public bool IsCompleted => _task.IsCompleted;

        DTask IDTaskAwaiter.Task => _task;

        private TaskAwaiter<bool> UnderlyingTaskAwaiter => _task.UnderlyingTask.GetAwaiter();

        public TResult GetResult()
        {
            _task.EnsureCompleted();
            return _task.Result;
        }

        public void OnCompleted(Action continuation) => UnderlyingTaskAwaiter.OnCompleted(continuation);

        public void UnsafeOnCompleted(Action continuation) => UnderlyingTaskAwaiter.UnsafeOnCompleted(continuation);
    }

    public new readonly struct DAwaiter
    {
        private readonly DTask<TResult> _task;

        internal DAwaiter(DTask<TResult> task)
        {
            _task = task;
        }

        public Task<bool> IsCompletedAsync()
        {
            return _task.UnderlyingTask;
        }

        public TResult GetResult()
        {
            _task.EnsureCompleted();
            return _task.Result;
        }

        public Task OnCompletedAsync<THandler>(ref THandler handler, CancellationToken cancellationToken = default)
            where THandler : ICompletionHandler
        {
            _task.EnsureCompleted();
            return handler.OnCompletedAsync(_task.Result, cancellationToken);
        }

        public Task OnSuspendedAsync<THandler>(ref THandler handler, CancellationToken cancellationToken = default)
            where THandler : ISuspensionHandler
        {
            _task.EnsureSuspended();
            return _task.OnSuspendedAsync(ref handler, cancellationToken);
        }
    }
}