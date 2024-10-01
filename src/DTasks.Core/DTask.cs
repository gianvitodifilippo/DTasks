using DTasks.CompilerServices;
using DTasks.Hosting;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DTasks;

[AsyncMethodBuilder(typeof(AsyncDTaskMethodBuilder))]
public abstract class DTask
{
    internal abstract Task<bool> UnderlyingTask { get; }

    internal abstract DTaskStatus Status { get; }

    internal bool IsRunning => Status is DTaskStatus.Running;

    internal bool IsCompleted => Status is DTaskStatus.RanToCompletion;

    internal bool IsSuspended => Status is DTaskStatus.Suspended;

    internal virtual bool IsStateful => false; // Allows internal optimizations

    [EditorBrowsable(EditorBrowsableState.Never)]
    public Awaiter GetAwaiter() => new Awaiter(this);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public DAwaiter GetDAwaiter() => new DAwaiter(this);

    internal virtual void SaveState<THandler>(ref THandler handler)
        where THandler : IStateHandler
    {
        Debug.Assert(!IsStateful, $"If a subclass of {nameof(DTask)} is declared as stateful, it should override the '{nameof(SaveState)}' method.");
    }

    internal abstract Task SuspendAsync<THandler>(ref THandler handler, CancellationToken cancellationToken)
        where THandler : ISuspensionHandler;

    internal virtual Task CompleteAsync<THandler>(ref THandler handler, CancellationToken cancellationToken)
        where THandler : ICompletionHandler
    {
        VerifyStatus(expectedStatus: DTaskStatus.RanToCompletion);
        return handler.OnCompletedAsync(cancellationToken);
    }

    public static DTask CompletedTask { get; } = new CompletedDTask<VoidDTaskResult>(default);

    public static DTask<TResult> FromResult<TResult>(TResult result) => new CompletedDTask<TResult>(result);

    public static DTask Yield() => YieldDTask.Instance;

    public static DTask Delay(TimeSpan delay) => new DelayDTask(delay);

    public static DTask WhenAll(IEnumerable<DTask> tasks) => new WhenAllDTask(tasks);

    public static DTask<TResult[]> WhenAll<TResult>(IEnumerable<DTask<TResult>> tasks) => new WhenAllDTask<TResult>(tasks);

    public static DTask Suspend(SuspensionCallback callback) => new DelegateSuspendedDTask<VoidDTaskResult>(callback);

    public static DTask Suspend<TState>(TState state, SuspensionCallback<TState> callback) => new DelegateSuspendedDTask<VoidDTaskResult, TState>(state, callback);

    public static DTask Suspend<TCallback>(TCallback callback) where TCallback : ISuspensionCallback
        => new SuspendedDTask<VoidDTaskResult, TCallback>(callback);

    public static DTask Suspend<TState, TCallback>(TState state, TCallback callback) where TCallback : ISuspensionCallback<TState>
        => new SuspendedDTask<VoidDTaskResult, TState, TCallback>(state, callback);

    private protected void EnsureCompleted()
    {
        if (!IsCompleted)
            throw new InvalidOperationException("The DTask was not completed.");
    }

    private protected void EnsureNotRunning()
    {
        if (IsRunning)
            throw new InvalidOperationException("The DTask was still running.");
    }

    private protected void EnsureSuspended()
    {
        if (!IsSuspended)
            throw new InvalidOperationException("The DTask was not suspended.");
    }

    [Conditional("DEBUG")]
    internal void AssertNotRunning()
    {
        Debug.Assert(!IsRunning, "The DTask was still running.");
    }

    [Conditional("DEBUG")]
    internal void AssertSuspended()
    {
        Debug.Assert(IsSuspended, $"The DTask was not suspended (it was '{Status}').");
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

    [DoesNotReturn]
    private protected static void InvalidAwait()
    {
        throw new InvalidOperationException("DTasks may be awaited in d-async methods only.");
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly struct Awaiter : ICriticalNotifyCompletion, IDTaskAwaiter
    {
        private readonly DTask _task;

        internal Awaiter(DTask task)
        {
            _task = task;
        }

        DTask IDTaskAwaiter.Task => _task;

        public bool IsCompleted => false;

        public void GetResult()
        {
            _task.EnsureCompleted();
        }

        public void OnCompleted(Action continuation) => InvalidAwait();

        public void UnsafeOnCompleted(Action continuation) => InvalidAwait();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
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

        public void SaveState<THandler>(ref THandler handler)
            where THandler : IStateHandler
        {
            _task.EnsureNotRunning();
            _task.SaveState(ref handler);
        }

        public Task SuspendAsync<THandler>(ref THandler handler, CancellationToken cancellationToken = default)
            where THandler : ISuspensionHandler
        {
            _task.EnsureSuspended();
            return _task.SuspendAsync(ref handler, cancellationToken);
        }

        public Task CompleteAsync<THandler>(ref THandler handler, CancellationToken cancellationToken = default)
            where THandler : ICompletionHandler
        {
            _task.EnsureCompleted();
            return _task.CompleteAsync(ref handler, cancellationToken);
        }
    }
}

[AsyncMethodBuilder(typeof(AsyncDTaskMethodBuilder<>))]
public abstract class DTask<TResult> : DTask
{
    internal abstract TResult Result { get; }

    internal override Task CompleteAsync<THandler>(ref THandler handler, CancellationToken cancellationToken)
    {
        VerifyStatus(expectedStatus: DTaskStatus.RanToCompletion);

        if (typeof(TResult) == typeof(VoidDTaskResult))
            return handler.OnCompletedAsync(cancellationToken);

        return handler.OnCompletedAsync(Result, cancellationToken);
    }

    public new static DTask<TResult> Suspend(SuspensionCallback callback) => new DelegateSuspendedDTask<TResult>(callback);

    public new static DTask<TResult> Suspend<TState>(TState state, SuspensionCallback<TState> callback) => new DelegateSuspendedDTask<TResult, TState>(state, callback);

    public new static DTask<TResult> Suspend<TCallback>(TCallback callback) where TCallback : ISuspensionCallback
        => new SuspendedDTask<TResult, TCallback>(callback);

    public new static DTask<TResult> Suspend<TState, TCallback>(TState state, TCallback callback) where TCallback : ISuspensionCallback<TState>
        => new SuspendedDTask<TResult, TState, TCallback>(state, callback);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public new Awaiter GetAwaiter() => new Awaiter(this);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public new DAwaiter GetDAwaiter() => new DAwaiter(this);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public new readonly struct Awaiter : ICriticalNotifyCompletion, IDTaskAwaiter
    {
        private readonly DTask<TResult> _task;

        internal Awaiter(DTask<TResult> task)
        {
            _task = task;
        }

        public bool IsCompleted => false;

        DTask IDTaskAwaiter.Task => _task;

        public TResult GetResult()
        {
            _task.EnsureCompleted();
            return _task.Result;
        }

        public void OnCompleted(Action continuation) => InvalidAwait();

        public void UnsafeOnCompleted(Action continuation) => InvalidAwait();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
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

        public void SaveState<THandler>(ref THandler handler)
            where THandler : IStateHandler
        {
            _task.EnsureNotRunning();
            _task.SaveState(ref handler);
        }

        public Task SuspendAsync<THandler>(ref THandler handler, CancellationToken cancellationToken = default)
            where THandler : ISuspensionHandler
        {
            _task.EnsureSuspended();
            return _task.SuspendAsync(ref handler, cancellationToken);
        }

        public Task CompleteAsync<THandler>(ref THandler handler, CancellationToken cancellationToken = default)
            where THandler : ICompletionHandler
        {
            _task.EnsureCompleted();
            return _task.CompleteAsync(ref handler, cancellationToken);
        }
    }
}
