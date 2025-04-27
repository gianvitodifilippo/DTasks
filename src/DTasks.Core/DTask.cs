using DTasks.CompilerServices;
using DTasks.Infrastructure;
using DTasks.Utils;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace DTasks;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
[AsyncMethodBuilder(typeof(AsyncDTaskMethodBuilder))]
public abstract class DTask : IDAsyncRunnable
{
    public abstract DTaskStatus Status { get; }

    public bool IsPending => Status is DTaskStatus.Pending;

    public bool IsRunning => Status is DTaskStatus.Running;

    public bool IsSucceeded => Status is DTaskStatus.Succeeded;

    public bool IsSuspended => Status is DTaskStatus.Suspended;

    public bool IsCanceled => Status is DTaskStatus.Canceled;

    public bool IsFaulted => Status is DTaskStatus.Faulted;

    public bool IsCompleted => Status is
        DTaskStatus.Succeeded or
        DTaskStatus.Faulted or
        DTaskStatus.Canceled;

    public bool IsFailed => Status is
        DTaskStatus.Faulted or
        DTaskStatus.Canceled;

    public Exception Exception
    {
        get
        {
            EnsureFailed();
            return ExceptionCore;
        }
    }

    internal Exception ExceptionInternal
    {
        get
        {
            AssertFailed();
            return ExceptionCore;
        }
    }

    internal CancellationToken CancellationTokenInternal
    {
        get
        {
            AssertCanceled();
            return CancellationTokenCore;
        }
    }

    protected virtual Exception ExceptionCore => throw new NotImplementedException($"If a DTask can be '{DTaskStatus.Faulted}' or '{DTaskStatus.Canceled}', then it should override {nameof(ExceptionCore)}.");

    protected virtual CancellationToken CancellationTokenCore => throw new NotImplementedException($"If a DTask can be '{DTaskStatus.Canceled}', then it should override {nameof(CancellationTokenCore)}.");

    internal virtual TReturn Accept<TReturn>(IDTaskVisitor<TReturn> visitor) => visitor.Visit(this);

    void IDAsyncRunnable.Run(IDAsyncRunner runner) => Run(runner);

    protected abstract void Run(IDAsyncRunner runner);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public Awaiter GetAwaiter() => new Awaiter(this);

    public static DTask CompletedDTask { get; } = new SucceededDTask();

    public static DTask<TResult> FromResult<TResult>(TResult result) => new SucceededDTask<TResult>(result);

    public static YieldDAwaitable Yield() => default;

    public static DTask Delay(TimeSpan delay) => new DelayDTask(delay);

    public static DTask FromException(Exception exception) => new FaultedDTask(exception);

    public static DTask FromCanceled(CancellationToken cancellationToken) => new CanceledDTask(cancellationToken);

    public static DTask WhenAll(params IEnumerable<DTask> tasks) => new WhenAllDTask(tasks);

    public static DTask<TResult[]> WhenAll<TResult>(params IEnumerable<DTask<TResult>> tasks) => new WhenAllDTask<TResult>(tasks);

    public static DTask<DTask> WhenAny(params IEnumerable<DTask> tasks) => new WhenAnyDTask(tasks);

    public static DTask<DTask<TResult>> WhenAny<TResult>(params IEnumerable<DTask<TResult>> tasks) => new WhenAnyDTask<TResult>(tasks);

    public static BackgroundDAwaitable Run(DTask task) => new(task);

    public static BackgroundDAwaitable<TResult> Run<TResult>(DTask<TResult> task) => new(task);

    public static DTaskFactory Factory { get; } = new();

    internal void EnsureCompleted()
    {
        Ensure(IsCompleted);
    }

    internal void EnsureSucceeded()
    {
        Ensure(IsSucceeded);
    }

    internal void EnsureFailed()
    {
        Ensure(IsFailed);
    }

    private static void Ensure([DoesNotReturnIf(false)] bool statusFlag)
    {
        if (!statusFlag)
            throw new InvalidOperationException("The operation attempted on a DTask was invalid given its state.");
    }

    [Conditional("DEBUG")]
    internal void AssertSucceeded()
    {
        Debug.Assert(IsSucceeded, $"The DTask was not succeeded (it was '{Status}').");
    }

    [Conditional("DEBUG")]
    internal void AssertFailed()
    {
        Debug.Assert(IsFailed, $"The DTask was not failed (it was '{Status}').");
    }

    [Conditional("DEBUG")]
    internal void AssertCanceled()
    {
        Debug.Assert(IsCanceled, $"The DTask was not canceled (it was '{Status}').");
    }

    [DoesNotReturn]
    internal static void ThrowInvalidAwait()
    {
        throw new InvalidOperationException("D-awaitables may be awaited in d-async methods only.");
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly struct Awaiter : ICriticalNotifyCompletion, IDAsyncAwaiter
    {
        private readonly DTask _task;

        internal Awaiter(DTask task)
        {
            _task = task;
        }

        public bool IsCompleted => false;

        bool IDAsyncAwaiter.IsCompleted => _task.IsCompleted;

        public void GetResult()
        {
            _task.EnsureCompleted();
            if (_task.IsSucceeded)
                return;

            _task.AssertFailed();
            ExceptionDispatchInfo.Throw(_task.ExceptionInternal);
        }

        void IDAsyncAwaiter.Continue(IDAsyncRunner runner)
        {
            _task.Run(runner);
        }

        public void OnCompleted(Action continuation)
        {
            ThrowHelper.ThrowIfNull(continuation);
            ThrowInvalidAwait();
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            ThrowHelper.ThrowIfNull(continuation);
            ThrowInvalidAwait();
        }
    }

    private string DebuggerDisplay => $"DTask (Status = {Status})";
}

[DebuggerDisplay("{DebuggerDisplay,nq}")]
[AsyncMethodBuilder(typeof(AsyncDTaskMethodBuilder<>))]
public abstract class DTask<TResult> : DTask
{
    public TResult Result
    {
        get
        {
            EnsureSucceeded();
            return ResultCore;
        }
    }

    internal TResult ResultInternal
    {
        get
        {
            AssertSucceeded();
            return ResultCore;
        }
    }

    protected virtual TResult ResultCore => throw new NotImplementedException($"If a DTask can be '{DTaskStatus.Succeeded}', then it should override {nameof(ResultCore)}.");

    internal override TReturn Accept<TReturn>(IDTaskVisitor<TReturn> visitor) => visitor.Visit(this);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public new Awaiter GetAwaiter() => new Awaiter(this);

    public new static DTask<TResult> FromException(Exception exception) => new FaultedDTask<TResult>(exception);

    public new static DTaskFactory<TResult> Factory { get; } = new();

    [EditorBrowsable(EditorBrowsableState.Never)]
    public new readonly struct Awaiter : ICriticalNotifyCompletion, IDAsyncAwaiter
    {
        private readonly DTask<TResult> _task;

        internal Awaiter(DTask<TResult> task)
        {
            _task = task;
        }

        public bool IsCompleted => false;

        bool IDAsyncAwaiter.IsCompleted => _task.IsCompleted;

        public TResult GetResult()
        {
            _task.EnsureCompleted();
            if (_task.IsSucceeded)
                return _task.ResultCore;

            _task.AssertFailed();
            ExceptionDispatchInfo.Throw(_task.ExceptionInternal);
            throw new UnreachableException();
        }

        void IDAsyncAwaiter.Continue(IDAsyncRunner runner)
        {
            _task.Run(runner);
        }

        public void OnCompleted(Action continuation)
        {
            ThrowHelper.ThrowIfNull(continuation);
            ThrowInvalidAwait();
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            ThrowHelper.ThrowIfNull(continuation);
            ThrowInvalidAwait();
        }
    }

    private string DebuggerDisplay => $"DTask (Status = {Status})";
}
