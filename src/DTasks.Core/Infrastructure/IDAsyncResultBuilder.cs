using System.ComponentModel;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncResultBuilder
{
    void SetResult();

    void SetException(Exception exception);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncResultBuilder<TResult>
{
    void SetResult(TResult result);

    void SetException(Exception exception);
}
