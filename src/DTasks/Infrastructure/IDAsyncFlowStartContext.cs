namespace DTasks.Infrastructure;

public interface IDAsyncFlowStartContext
{
    void SetResult();
    
    void SetException(Exception exception);
    
    DAsyncId FlowId { get; }
}