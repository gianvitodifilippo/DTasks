using DTasks.Infrastructure;
using System.ComponentModel;

namespace DTasks.CompilerServices;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncAwaiter
{
    bool IsCompleted { get; }

    void Continue(IDAsyncRunner runner);
}
