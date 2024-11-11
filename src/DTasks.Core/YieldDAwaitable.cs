using DTasks.CompilerServices;
using DTasks.Hosting;
using DTasks.Utils;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DTasks;

[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct YieldDAwaitable
{
    public Awaiter GetAwaiter() => default;

    public readonly struct Awaiter : ICriticalNotifyCompletion, IDAsyncAwaiter
    {
        public bool IsCompleted => false;

        bool IDAsyncAwaiter.IsCompleted => false;

        public void GetResult() { }

        public void Continue(IDAsyncFlow flow) => flow.Yield();

        public void OnCompleted(Action continuation)
        {
            ThrowHelper.ThrowIfNull(continuation);
            DTask.InvalidAwait();
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            ThrowHelper.ThrowIfNull(continuation);
            DTask.InvalidAwait();
        }
    }
}
