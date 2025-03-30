using System.ComponentModel;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncRunnable
{
    void Run(IDAsyncRunner runner);
}
