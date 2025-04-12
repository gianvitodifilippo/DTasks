using System.ComponentModel;

namespace DTasks.Infrastructure.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface ISuspensionContext
{
    IDAsyncMarshaler Marshaler { get; }
    
    bool IsSuspended<TAwaiter>(ref TAwaiter awaiter);
}
