using System.ComponentModel;

namespace DTasks.Hosting;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncRunnable
{
    void Run(IDAsyncFlow flow);
}
