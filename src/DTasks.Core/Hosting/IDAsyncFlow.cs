using System.ComponentModel;

namespace DTasks.Hosting;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncFlow
{
    void Start(IDAsyncStateMachine stateMachine);

    void Resume();

    void Resume<TResult>(TResult result);

    void Resume(Exception exception);

    void Yield();

    void Delay(TimeSpan delay);

    void WhenAll(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultCallback callback);

    void WhenAll<TResult>(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultCallback<TResult[]> callback);

    void WhenAny(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultCallback<DTask> callback);

    void WhenAny<TResult>(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultCallback<DTask<TResult>> callback);

    void Background(IDAsyncRunnable runnable, IDAsyncResultCallback<DTask> callback);

    void Background<TResult>(IDAsyncRunnable runnable, IDAsyncResultCallback<DTask<TResult>> callback);
}
