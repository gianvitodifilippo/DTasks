using System.ComponentModel;
using System.Runtime.CompilerServices;
using DTasks.Infrastructure;
using DTasks.Utils;

namespace DTasks.Execution;

[EditorBrowsable(EditorBrowsableState.Never)]
public readonly ref struct CancellationDAwaitable
{
    private readonly DCancellationSourceDAsyncBuilder _builder;

    internal CancellationDAwaitable(DCancellationSourceDAsyncBuilder builder)
    {
        _builder = builder;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public Awaiter GetAwaiter() => new(_builder);

    public readonly struct Awaiter : ICriticalNotifyCompletion, IDAsyncAwaiter
    {
        private readonly DCancellationSourceDAsyncBuilder _builder;

        internal Awaiter(DCancellationSourceDAsyncBuilder builder)
        {
            _builder = builder;
        }

        public bool IsCompleted => _builder.IsCompleted;

        bool IDAsyncAwaiter.IsCompleted => false;

        public DCancellationTokenSource GetResult() => _builder.GetResult();

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
            _builder.Continue(runner);
        }
    }
}