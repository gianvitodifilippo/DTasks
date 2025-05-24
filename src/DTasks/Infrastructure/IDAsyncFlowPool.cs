namespace DTasks.Infrastructure;

internal interface IDAsyncFlowPool
{
#if DEBUG
    DAsyncFlow Get(IDAsyncHost host, string? stackTrace = null);
#else
    DAsyncFlow Get(IDAsyncHost host);
#endif

    void Return(DAsyncFlow flow);
}
