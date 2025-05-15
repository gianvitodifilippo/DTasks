using DTasks.Configuration;

namespace DTasks.Infrastructure;

internal interface IDAsyncFlowPool
{
#if DEBUG
    DAsyncFlow Get(IDAsyncHost host, string? stackTrace);
#else
    DAsyncFlow Get(IDAsyncHost host, bool returnToPool);
#endif

    void Return(DAsyncFlow flow);
}

internal static class DAsyncFlowPoolExtensions
{
    public static DAsyncFlow UnsafeGet(this IDAsyncFlowPool pool, IDAsyncHost host)
    {
#if DEBUG
        return pool.Get(host, stackTrace: string.Empty);
#else
        return pool.Get(host, returnToPool: false);
#endif
    }
}