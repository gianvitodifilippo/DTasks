using System.ComponentModel;

namespace DTasks.Infrastructure.State;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface ILinkContext
{
    DAsyncId Id { get; }
    
    DAsyncId ParentId { get; }

    void SetResult();
    
    void SetResult<TResult>(TResult result);
    
    void SetException(Exception exception);
}