using System.ComponentModel;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncFlowCompletionContext : IDAsyncFlowContext
{
    DAsyncId FlowId { get; }
}