using System.ComponentModel;

namespace DTasks.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface ISuspensionContext
{
    bool IsSuspended<TAwaiter>(ref TAwaiter awaiter);
}
