using System.ComponentModel;

namespace DTasks.Infrastructure.State;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDehydrationContext
{
    DAsyncId ParentId { get; }
    
    DAsyncId Id { get; }
    
    bool IsSuspended<TAwaiter>(ref TAwaiter awaiter);
}
