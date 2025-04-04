using System.ComponentModel;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncAwaiter
{
    bool IsCompleted { get; }

    void Continue(IDAsyncRunner runner);
}
