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

    internal static DCancellationTokenSource Create(IDAsyncCancellationManager manager)
    {
        return new Implementation(manager);
    }

    public static CancellationDAwaitable CreateAsync(CancellationToken cancellationToken = default) => new(new CreationAwaiter(cancellationToken));

    public static CancellationDAwaitable CreateAsync(TimeSpan delay, CancellationToken cancellationToken = default) => new(new CreationAwaiterWithDelay(delay, cancellationToken));

    private sealed class Implementation(IDAsyncCancellationManager manager) : DCancellationTokenSource
    {
        public override bool IsCancellationRequested => LocalSource.IsCancellationRequested;

        internal override CancellationTokenSource LocalSource { get; } = new();

        private protected override DAsyncCancellationHandle Handle => new(LocalSource);

        private protected override Task CancelCoreAsync(CancellationToken cancellationToken)
        {
            return manager.CancelAsync(this, cancellationToken);
        }

        private protected override Task CancelAfterCoreAsync(TimeSpan delay, CancellationToken cancellationToken)
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
