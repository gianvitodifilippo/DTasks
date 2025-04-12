using DTasks.Execution;

namespace DTasks.Infrastructure;

internal interface IDAsyncRunnerInternal : IDAsyncRunner
{
    void Callback(ISuspensionCallback callback);

    void Handle(DAsyncId id, IDAsyncResultBuilder builder);

    void Handle<TResult>(DAsyncId id, IDAsyncResultBuilder<TResult> builder);
}
