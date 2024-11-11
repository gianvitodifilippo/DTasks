using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DTasks.Hosting;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncMethodBuilder
{
    void AwaitOnCompleted<TAwaiter>(ref TAwaiter awaiter)
        where TAwaiter : INotifyCompletion;

    void AwaitUnsafeOnCompleted<TAwaiter>(ref TAwaiter awaiter)
        where TAwaiter : ICriticalNotifyCompletion;

    void SetResult();

    void SetResult<TResult>(TResult result);

    void SetException(Exception exception);

    void SetState<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : notnull;
}
