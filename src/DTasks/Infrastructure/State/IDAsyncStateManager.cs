using System.ComponentModel;

namespace DTasks.Infrastructure.State;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncStateManager
{
    IDAsyncStack Stack { get; }
    
    IDAsyncHeap Heap { get; }
}