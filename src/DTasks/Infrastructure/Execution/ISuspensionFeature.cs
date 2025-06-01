using System.ComponentModel;
using DTasks.Execution;

namespace DTasks.Infrastructure.Execution;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface ISuspensionFeature
{
    void Suspend(ISuspensionCallback callback);
}
