namespace DTasks.Hosting;

internal interface IDAsyncFlowInternal : IDAsyncFlow
{
    void Callback(ISuspensionCallback callback);

    void Handle(DAsyncId id);

    void Handle<TResult>(DAsyncId id);
}
