using System.ComponentModel;

namespace DTasks.Infrastructure.Execution;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncCancellationHandler
{
    void Cancel(DCancellationId id);
}