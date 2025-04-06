using System.ComponentModel;

namespace DTasks.Execution;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncCancellationHandler
{
    void Cancel(DCancellationId id);
}