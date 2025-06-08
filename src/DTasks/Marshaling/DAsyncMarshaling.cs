using System.ComponentModel;
using System.Runtime.CompilerServices;
using DTasks.Infrastructure;
using DTasks.Infrastructure.Marshaling;
using DTasks.Utils;

namespace DTasks.Marshaling;

public static class DAsyncMarshaling
{
    public static MarshalDTaskAwaitable MarshalDAsync(this DTask task) => new(task);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly struct MarshalDTaskAwaitable(DTask task) : IDAsyncAwaiter, ICriticalNotifyCompletion
    {
        public bool IsCompleted => false;

        public MarshalDTaskAwaitable GetAwaiter() => this;

        public void GetResult()
        {
            // We don't have a result to give back or an exception to throw
        }

        public void Continue(IDAsyncRunner runner)
        {
            var feature = runner.Features.GetRequiredFeature<IMarshalingFeature>();
            feature.Marshal(task);
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
        
        public static MarshalDTaskAwaitable FromResult() => default;
    }
}