namespace DTasks.Infrastructure;

public interface IDAsyncFlowCompletionContext
{
    DAsyncId FlowId { get; }
}