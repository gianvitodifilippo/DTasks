using System.ComponentModel;

namespace DTasks.Hosting;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncResultCallback
{
    void SetResult();

    void SetException(Exception exception);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncResultCallback<TResult>
{
    void SetResult(TResult result);

    void SetException(Exception exception);
}
