using System.ComponentModel;

namespace DTasks.Hosting;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncFlow
{
    void Start(IDAsyncStateMachine stateMachine);

    void Succeed();

    void Succeed<TResult>(TResult result);

    void Fail(Exception exception);

    void Yield();

    void Delay(TimeSpan delay);

    void WhenAll(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultCallback callback);

    void WhenAll<TResult>(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultCallback<TResult[]> callback);

    void WhenAny(IEnumerable<IDAsyncRunnable> tasks, IDAsyncResultCallback<DTask> callback);

    void WhenAny<TResult>(IEnumerable<IDAsyncRunnable> tasks, IDAsyncResultCallback<DTask<TResult>> callback);

    void Run(IDAsyncRunnable runnable, IDAsyncResultCallback<DTask> callback);

    void Run<TResult>(IDAsyncRunnable runnable, IDAsyncResultCallback<DTask<TResult>> callback);
}
