using Microsoft.Extensions.ObjectPool;

namespace DTasks.Infrastructure;

internal sealed class DAsyncFlowPool : IDAsyncFlowPool
{
    private readonly IDAsyncInfrastructure _infrastructure;
    private readonly DefaultObjectPool<DAsyncFlow> _pool;

    public DAsyncFlowPool(IDAsyncInfrastructure infrastructure)
    {
        _infrastructure = infrastructure;
        _pool = new DefaultObjectPool<DAsyncFlow>(new Policy(this));
    }

#if DEBUG
    public DAsyncFlow Get(IDAsyncHost host, string? stackTrace)
    {
        DAsyncFlow flow = _pool.Get();
        flow.Initialize(host, stackTrace);

        return flow;
    }
#else
    public DAsyncFlow Get(IDAsyncHost host)
    {
        DAsyncFlow flow = _pool.Get();
        flow.Initialize(host);

        return flow;
    }
#endif

    public void Return(DAsyncFlow flow) => _pool.Return(flow);

    private sealed class Policy(DAsyncFlowPool pool) : IPooledObjectPolicy<DAsyncFlow>
    {
        public DAsyncFlow Create() => new(pool, pool._infrastructure, DAsyncIdFactory.Default);

        public bool Return(DAsyncFlow obj) => true;
    }
}
