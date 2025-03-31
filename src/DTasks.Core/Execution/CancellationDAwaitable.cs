using DTasks.Infrastructure;
using DTasks.Utils;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DTasks.Execution;

[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct CancellationDAwaitable
{
    private readonly CancellationFactoryArguments _arguments;
    private readonly Func<CancellationFactoryArguments, IDAsyncCancellationManager, DCancellationTokenSource> _createSource;

    internal CancellationDAwaitable(CancellationFactoryArguments arguments, Func<CancellationFactoryArguments, IDAsyncCancellationManager, DCancellationTokenSource> createSource)
    {
        _arguments = arguments;
        _createSource = createSource;
    }

    public Awaiter GetAwaiter() => new(_arguments, _createSource);

    public struct Awaiter : ICriticalNotifyCompletion, IDAsyncAwaiter
    {
        private readonly CancellationFactoryArguments _arguments;
        private readonly Func<CancellationFactoryArguments, IDAsyncCancellationManager, DCancellationTokenSource> _createSource;
        private DCancellationTokenSource? _source;

        internal Awaiter(CancellationFactoryArguments arguments, Func<CancellationFactoryArguments, IDAsyncCancellationManager, DCancellationTokenSource> createSource)
        {
            _arguments = arguments;
            _createSource = createSource;
        }

        [MemberNotNullWhen(true, nameof(_source))]
        public readonly bool IsCompleted => _source is not null;

        public readonly DCancellationTokenSource GetResult()
        {
            return IsCompleted
                ? _source
                : throw new InvalidOperationException($"Attempted to create an instance of {nameof(DCancellationTokenSource)} without awaiting the call.");
        }

        public readonly void UnsafeOnCompleted(Action continuation)
        {
            ThrowHelper.ThrowIfNull(continuation);
            DTask.ThrowInvalidAwait();
        }

        public readonly void OnCompleted(Action continuation)
        {
            ThrowHelper.ThrowIfNull(continuation);
            DTask.ThrowInvalidAwait();
        }

        readonly bool IDAsyncAwaiter.IsCompleted => false;

        void IDAsyncAwaiter.Continue(IDAsyncRunner runner)
        {
            _source = _createSource(_arguments, runner.CancellationFactory);
        }
    }
}
