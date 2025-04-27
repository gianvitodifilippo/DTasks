using System.ComponentModel;

namespace DTasks.Infrastructure.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface ISuspensionContext
{
    bool IsSuspended<TAwaiter>(ref TAwaiter awaiter);
}
