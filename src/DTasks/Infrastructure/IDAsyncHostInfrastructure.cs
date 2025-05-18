using System.ComponentModel;
using DTasks.Infrastructure.State;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncHostInfrastructure
{
    IDAsyncRootInfrastructure Parent { get; }

    IDAsyncHeap GetHeap();
}