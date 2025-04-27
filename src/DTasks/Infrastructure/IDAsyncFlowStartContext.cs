using System.ComponentModel;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncFlowStartContext : IDAsyncFlowContext
{
    void SetResult();

    void SetException(Exception exception);

    DAsyncId FlowId { get; }
}