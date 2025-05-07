using DTasks.Configuration;
using Microsoft.Extensions.ObjectPool;

namespace DTasks.Infrastructure;

internal sealed class DAsyncFlowPool : IDAsyncFlowPool
{
    private readonly DefaultObjectPool<DAsyncFlow> _pool;

    public DAsyncFlowPool()
    {
        _pool = new DefaultObjectPool<DAsyncFlow>(new Policy(this));
    }

#if DEBUG
    public DAsyncFlow Get(IDAsyncInfrastructure infrastructure, string? stackTrace)
    {
        DAsyncFlow flow = _pool.Get();
        flow.Initialize(infrastructure, stackTrace);

        return flow;
    }
#else
    public DAsyncFlow Get(IDAsyncInfrastructure infrastructure, bool returnToPool)
    {
        DAsyncFlow flow = _pool.Get();
        flow.Initialize(infrastructure, returnToPool);

        return flow;
    }
#endif

    public void Return(DAsyncFlow flow) => _pool.Return(flow);

    private sealed class Policy(DAsyncFlowPool pool) : IPooledObjectPolicy<DAsyncFlow>
    {
        public DAsyncFlow Create() => DAsyncFlow.Create(pool);

        public bool Return(DAsyncFlow obj) => true;
    }
}
