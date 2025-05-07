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

    public DAsyncRunner CreateRunner()
    {
#if DEBUG
        return _flowPool.Get(_infrastructure, Environment.StackTrace);
#else
        return _flowPool.Get(_infrastructure, returnToPool: false);
#endif
    }

    public ValueTask StartAsync(IDAsyncHost host, IDAsyncRunnable runnable, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(host);
        ThrowHelper.ThrowIfNull(runnable);

        DAsyncFlow flow = GetFlow();
        return flow.StartAsync(host, runnable, cancellationToken);
    }

    public ValueTask ResumeAsync(IDAsyncHost host, DAsyncId id, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(host);

        DAsyncFlow flow = GetFlow();
        return flow.ResumeAsync(host, id, cancellationToken);
    }

    public ValueTask ResumeAsync<TResult>(IDAsyncHost host, DAsyncId id, TResult result, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(host);

        DAsyncFlow flow = GetFlow();
        return flow.ResumeAsync(host, id, result, cancellationToken);
    }

    public ValueTask ResumeAsync(IDAsyncHost host, DAsyncId id, Exception exception, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(host);

        DAsyncFlow flow = GetFlow();
        return flow.ResumeAsync(host, id, exception, cancellationToken);
    }

    private DAsyncFlow GetFlow()
    {
#if DEBUG
        return _flowPool.Get(_infrastructure, stackTrace: null);
#else
        return _flowPool.Get(_infrastructure, returnToPool: true);
#endif
    }

    public static DTasksConfiguration Build(Action<IDTasksConfigurationBuilder> configure)
    {
        DTasksConfigurationBuilder builder = new();
        DAsyncFlow.ConfigureMarshaling(builder);
        configure(builder);

        return builder.Build();
    }
}
