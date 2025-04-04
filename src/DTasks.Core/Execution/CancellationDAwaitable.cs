using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using DTasks.Infrastructure;
using DTasks.Utils;

namespace DTasks.Execution;

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

        private protected Awaiter()
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
            _result = DCancellationTokenSource.Create(runner.Cancellation);
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