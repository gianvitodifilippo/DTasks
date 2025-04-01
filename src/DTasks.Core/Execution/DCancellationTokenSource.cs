using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using DTasks.Infrastructure;
using DTasks.Utils;

namespace DTasks.Execution;

public abstract class DCancellationTokenSource
{
    internal static readonly DCancellationTokenSource CanceledSource = new Canceled();
    internal static readonly DCancellationTokenSource NeverCanceledSource = new NeverCanceled();

    private DCancellationTokenSource()
    {
    }

    public DCancellationToken Token => new(this);

    public abstract bool IsCancellationRequested { get; }

    internal abstract CancellationTokenSource LocalSource { get; }

    private protected abstract DAsyncCancellationHandle Handle { get; }

    private protected abstract Task CancelCoreAsync(CancellationToken cancellationToken);

    private protected abstract Task CancelAfterCoreAsync(TimeSpan delay, CancellationToken cancellationToken);

    public Task CancelAsync(CancellationToken cancellationToken = default)
    {
        return CancelCoreAsync(cancellationToken);
    }

    public Task CancelAfterAsync(TimeSpan delay, CancellationToken cancellationToken = default)
    {
        return CancelAfterCoreAsync(delay, cancellationToken);
    }

    public static CancellationDAwaitable CreateAsync(CancellationToken cancellationToken = default) => new(new CreationAwaiter(cancellationToken));

    public static CancellationDAwaitable CreateAsync(TimeSpan delay, CancellationToken cancellationToken = default) => new(new CreationAwaiterWithDelay(delay, cancellationToken));

    private sealed class Implementation(IDAsyncCancellationManager manager) : DCancellationTokenSource
    {
        public override bool IsCancellationRequested => manager.IsCancellationRequested(this);

        internal override CancellationTokenSource LocalSource { get; } = new();

        private protected override DAsyncCancellationHandle Handle => new(LocalSource);

        private protected override Task CancelCoreAsync(CancellationToken cancellationToken = default)
        {
            return manager.CancelAsync(this, cancellationToken);
        }

        private protected override Task CancelAfterCoreAsync(TimeSpan delay, CancellationToken cancellationToken = default)
        {
            return manager.CancelAfterAsync(this, delay, cancellationToken);
        }
    }

    private sealed class Canceled : DCancellationTokenSource
    {
        public Canceled()
        {
            LocalSource = new();
            LocalSource.Cancel();
        }

        public override bool IsCancellationRequested => true;

        internal override CancellationTokenSource LocalSource { get; }

        private protected override DAsyncCancellationHandle Handle
        {
            get
            {
                Debug.Fail($"'{nameof(Handle)}' should not be used for this source.");
                return default;
            }
        }

        private protected override Task CancelCoreAsync(CancellationToken cancellationToken)
        {
            Debug.Fail($"'{nameof(CancelAsync)}' should not be used for this source.");
            return Task.CompletedTask;
        }

        private protected override Task CancelAfterCoreAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            Debug.Fail($"'{nameof(CancelAfterAsync)}' should not be used for this source.");
            return Task.CompletedTask;
        }
    }

    private sealed class NeverCanceled : DCancellationTokenSource
    {
        internal override CancellationTokenSource LocalSource { get; } = new();

        public override bool IsCancellationRequested => false;

        private protected override DAsyncCancellationHandle Handle
        {
            get
            {
                Debug.Fail($"'{nameof(Handle)}' should not be used for this source.");
                return default;
            }
        }

        private protected override Task CancelCoreAsync(CancellationToken cancellationToken)
        {
            Debug.Fail($"'{nameof(CancelAsync)}' should not be used for this source.");
            return Task.CompletedTask;
        }

        private protected override Task CancelAfterCoreAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            Debug.Fail($"'{nameof(CancelAfterAsync)}' should not be used for this source.");
            return Task.CompletedTask;
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly ref struct CancellationDAwaitable
    {
        private readonly Awaiter _awaiter;

        internal CancellationDAwaitable(Awaiter awaiter)
        {
            _awaiter = awaiter;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Awaiter GetAwaiter() => _awaiter;

        public abstract class Awaiter : ICriticalNotifyCompletion, IDAsyncAwaiter, IDAsyncResultBuilder<Task>
        {
            private DCancellationTokenSource? _result;
            private Exception? _exception;

            internal Awaiter()
            {
            }

            public bool IsCompleted { get; private set; }

            bool IDAsyncAwaiter.IsCompleted => false;

            private protected abstract Task CreateAsync(IDAsyncCancellationManager manager, DCancellationTokenSource source);

            public DCancellationTokenSource GetResult()
            {
                if (!IsCompleted)
                    throw new InvalidOperationException($"Attempted to create an instance of {nameof(DCancellationTokenSource)} without awaiting the call.");
            
                if (_exception is not null)
                {
                    ExceptionDispatchInfo.Throw(_exception);
                    throw new UnreachableException();
                }

                Debug.Assert(_result is not null);
                return _result;
            }
            public void OnCompleted(Action continuation)
            {
                ThrowHelper.ThrowIfNull(continuation);
                DTask.ThrowInvalidAwait();
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                ThrowHelper.ThrowIfNull(continuation);
                DTask.ThrowInvalidAwait();
            }

            void IDAsyncAwaiter.Continue(IDAsyncRunner runner)
            {
                _result = new Implementation(runner.Cancellation);
                runner.Await(CreateAsync(runner.Cancellation, _result), this);
            }

            public void SetResult(Task result)
            {
                IsCompleted = true;
            }

            public void SetException(Exception exception)
            {
                IsCompleted = true;
                _exception = exception;
            }
        }
    }

    private sealed class CreationAwaiter(CancellationToken cancellationToken) : CancellationDAwaitable.Awaiter
    {
        private protected override Task CreateAsync(IDAsyncCancellationManager manager, DCancellationTokenSource source)
        {
            return manager.CreateAsync(source, source.Handle, cancellationToken);
        }
    }

    private sealed class CreationAwaiterWithDelay(TimeSpan delay, CancellationToken cancellationToken) : CancellationDAwaitable.Awaiter
    {
        private protected override Task CreateAsync(IDAsyncCancellationManager manager, DCancellationTokenSource source)
        {
            return manager.CreateAsync(source, source.Handle, delay, cancellationToken);
        }
    }
}
