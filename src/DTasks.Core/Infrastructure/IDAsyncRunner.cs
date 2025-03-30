using System.ComponentModel;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncRunner
{
    void Start(IDAsyncStateMachine stateMachine);

    void Succeed();

    void Succeed<TResult>(TResult result);

    void Fail(Exception exception);

    void Yield();

    void Delay(TimeSpan delay);

    void WhenAll(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultBuilder builder);

    void WhenAll<TResult>(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultBuilder<TResult[]> builder);

    void WhenAny(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultBuilder<DTask> builder);

    void WhenAny<TResult>(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultBuilder<DTask<TResult>> builder);

    void Background(IDAsyncRunnable runnable, IDAsyncResultBuilder<DTask> builder);

    void Background<TResult>(IDAsyncRunnable runnable, IDAsyncResultBuilder<DTask<TResult>> builder);
}
