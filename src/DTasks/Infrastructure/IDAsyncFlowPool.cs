using DTasks.Configuration;

namespace DTasks.Infrastructure;

internal interface IDAsyncFlowPool
{
#if DEBUG
    DAsyncFlow Get(IDAsyncInfrastructure infrastructure, string? stackTrace);
#else
    DAsyncFlow Get(IDAsyncInfrastructure infrastructure, bool returnToPool);
#endif

    void Return(DAsyncFlow flow);
}

internal static class DAsyncFlowPoolExtensions
{
    public static DAsyncFlow UnsafeGet(this IDAsyncFlowPool pool, IDAsyncInfrastructure infrastructure)
    {
#if DEBUG
        return pool.Get(infrastructure, stackTrace: string.Empty);
#else
        return pool.Get(infrastructure, returnToPool: false);
#endif
    }
}