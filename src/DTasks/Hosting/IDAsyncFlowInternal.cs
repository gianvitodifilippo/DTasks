namespace DTasks.Hosting;

internal interface IDAsyncFlowInternal : IDAsyncFlow
{
    void Callback(ISuspensionCallback callback);

    void Handle(DAsyncId id, IDAsyncResultBuilder builder);

    void Handle<TResult>(DAsyncId id, IDAsyncResultBuilder<TResult> builder);
}
