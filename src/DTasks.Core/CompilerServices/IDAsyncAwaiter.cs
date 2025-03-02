using DTasks.Hosting;
using System.ComponentModel;

namespace DTasks.CompilerServices;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncAwaiter
{
    bool IsCompleted { get; }

    void Continue(IDAsyncFlow flow);
}
