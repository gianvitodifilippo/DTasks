using System.ComponentModel;

namespace DTasks.Hosting;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface ISuspensionContext
{
    bool IsSuspended<TAwaiter>(ref TAwaiter awaiter);
}
