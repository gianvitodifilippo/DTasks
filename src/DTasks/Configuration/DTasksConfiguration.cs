using DTasks.Infrastructure;
using DTasks.Utils;

namespace DTasks.Configuration;

public sealed class DTasksConfiguration
{
    private readonly IDAsyncFlowPool _flowPool;
    private readonly IDAsyncInfrastructure _infrastructure;

    internal DTasksConfiguration(IDAsyncFlowPool flowPool, IDAsyncInfrastructure infrastructure)
    {
        _flowPool = flowPool;
        _infrastructure = infrastructure;
    }
    
    public IDAsyncRootInfrastructure Infrastructure => _infrastructure.RootInfrastructure;

    public IDAsyncHostInfrastructure CreateHostInfrastructure(IDAsyncHost host)
    {
        return new DAsyncHostInfrastructure(_infrastructure, host);
    }

    public DAsyncRunner CreateRunner(IDAsyncHost host)
    {
#if DEBUG
        return _flowPool.Get(host, Environment.StackTrace);
#else
        return _flowPool.Get(host);
#endif
    }

    public async Task StartAsync(IDAsyncHost host, IDAsyncRunnable runnable, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(host);
        ThrowHelper.ThrowIfNull(runnable);

        using DAsyncFlow flow = _flowPool.Get(host);
        await flow.StartAsync(runnable, cancellationToken);
    }

    public async Task ResumeAsync(IDAsyncHost host, DAsyncId id, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(host);

        using DAsyncFlow flow = _flowPool.Get(host);
        await flow.ResumeAsync(id, cancellationToken);
    }

    public async Task ResumeAsync<TResult>(IDAsyncHost host, DAsyncId id, TResult result, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(host);

        using DAsyncFlow flow = _flowPool.Get(host);
        await flow.ResumeAsync(id, result, cancellationToken);
    }

    public async Task ResumeAsync(IDAsyncHost host, DAsyncId id, Exception exception, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(host);

        using DAsyncFlow flow = _flowPool.Get(host);
        await flow.ResumeAsync(id, exception, cancellationToken);
    }

    public static DTasksConfiguration Build(Action<IDTasksConfigurationBuilder> configure)
    {
        DTasksConfigurationBuilder builder = new();
        DAsyncFlow.ConfigureMarshaling(builder);
        configure(builder);

        return builder.Build();
    }
}
