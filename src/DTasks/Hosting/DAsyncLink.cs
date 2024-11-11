using System.ComponentModel;

namespace DTasks.Hosting;

[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct DAsyncLink(DAsyncId parentId, IDAsyncRunnable runnable)
{
    public DAsyncId ParentId { get; } = parentId;

    public IDAsyncRunnable Runnable { get; } = runnable;

    public void Deconstruct(out DAsyncId parentId, out IDAsyncRunnable runnable)
    {
        parentId = ParentId;
        runnable = Runnable;
    }
}
