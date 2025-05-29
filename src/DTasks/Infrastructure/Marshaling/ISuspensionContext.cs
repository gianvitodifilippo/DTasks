using System.ComponentModel;

namespace DTasks.Infrastructure.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface ISuspensionContext
{
    DAsyncId ParentId { get; }
    
    DAsyncId Id { get; }
    
    bool IsSuspended<TAwaiter>(ref TAwaiter awaiter);
}
