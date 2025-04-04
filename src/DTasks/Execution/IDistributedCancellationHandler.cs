using System.ComponentModel;

namespace DTasks.Execution;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDistributedCancellationHandler
{
    void Cancel(DCancellationId id);
}