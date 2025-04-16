using System.ComponentModel;

namespace DTasks.Infrastructure.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface ISuspensionContext
{
    IDAsyncSurrogator Surrogator { get; }
    
    bool IsSuspended<TAwaiter>(ref TAwaiter awaiter);
}
