using System.ComponentModel;
using DTasks.Utils;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class DAsyncRunner : IDisposable
{
    protected abstract ValueTask StartCoreAsync(IDAsyncHost host, IDAsyncRunnable runnable, CancellationToken cancellationToken);

    protected abstract ValueTask ResumeCoreAsync(IDAsyncHost host, DAsyncId id, CancellationToken cancellationToken);

    protected abstract ValueTask ResumeCoreAsync<TResult>(IDAsyncHost host, DAsyncId id, TResult result, CancellationToken cancellationToken);

    protected abstract ValueTask ResumeCoreAsync(IDAsyncHost host, DAsyncId id, Exception exception, CancellationToken cancellationToken);

    public abstract void Dispose();

    public ValueTask StartAsync(IDAsyncHost host, IDAsyncRunnable runnable, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(host);
        ThrowHelper.ThrowIfNull(runnable);

        return StartCoreAsync(host, runnable, cancellationToken);
    }

    public ValueTask ResumeAsync(IDAsyncHost host, DAsyncId id, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(host);

        return ResumeCoreAsync(host, id, cancellationToken);
    }

    public ValueTask ResumeAsync<TResult>(IDAsyncHost host, DAsyncId id, TResult result, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(host);

        return ResumeCoreAsync(host, id, result, cancellationToken);
    }

    public ValueTask ResumeAsync(IDAsyncHost host, DAsyncId id, Exception exception, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(host);
        ThrowHelper.ThrowIfNull(exception);

        return ResumeCoreAsync(host, id, exception, cancellationToken);
    }

    public static ValueTask StartFlowAsync(IDAsyncHost host, IDAsyncRunnable runnable, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(host);
        ThrowHelper.ThrowIfNull(runnable);

        DAsyncFlow flow = DAsyncFlow.RentFromCache(returnToCache: true);

        return flow.StartCoreAsync(host, runnable, cancellationToken);
    }

    public static ValueTask ResumeFlowAsync(IDAsyncHost host, DAsyncId id, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(host);

        DAsyncFlow flow = DAsyncFlow.RentFromCache(returnToCache: true);
        return flow.ResumeCoreAsync(host, id, cancellationToken);
    }

    public static ValueTask ResumeFlowAsync<TResult>(IDAsyncHost host, DAsyncId id, TResult result, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(host);

        DAsyncFlow flow = DAsyncFlow.RentFromCache(returnToCache: true);
        return flow.ResumeCoreAsync(host, id, result, cancellationToken);
    }

    public static ValueTask ResumeFlowAsync(IDAsyncHost host, DAsyncId id, Exception exception, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(host);

        DAsyncFlow flow = DAsyncFlow.RentFromCache(returnToCache: true);
        return flow.ResumeCoreAsync(host, id, exception, cancellationToken);
    }

    public static ValueTask StartFlowAsync(IDAsyncHostFactory hostFactory, IDAsyncRunnable runnable, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(hostFactory);
        ThrowHelper.ThrowIfNull(runnable);

        DAsyncFlow flow = DAsyncFlow.RentFromCache(returnToCache: true);
        IDAsyncHost host = hostFactory.CreateHost(flow);
        return flow.StartCoreAsync(host, runnable, cancellationToken);
    }

    public static ValueTask ResumeFlowAsync(IDAsyncHostFactory hostFactory, DAsyncId id, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(hostFactory);

        DAsyncFlow flow = DAsyncFlow.RentFromCache(returnToCache: true);
        IDAsyncHost host = hostFactory.CreateHost(flow);
        return flow.ResumeCoreAsync(host, id, cancellationToken);
    }

    public static ValueTask ResumeFlowAsync<TResult>(IDAsyncHostFactory hostFactory, DAsyncId id, TResult result, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(hostFactory);

        DAsyncFlow flow = DAsyncFlow.RentFromCache(returnToCache: true);
        IDAsyncHost host = hostFactory.CreateHost(flow);
        return flow.ResumeCoreAsync(host, id, result, cancellationToken);
    }

    public static ValueTask ResumeFlowAsync(IDAsyncHostFactory hostFactory, DAsyncId id, Exception exception, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(hostFactory);

        DAsyncFlow flow = DAsyncFlow.RentFromCache(returnToCache: true);
        IDAsyncHost host = hostFactory.CreateHost(flow);
        return flow.ResumeCoreAsync(host, id, exception, cancellationToken);
    }

    public static DAsyncRunner Create()
    {
#if DEBUG
        return DAsyncFlow.Create(Environment.StackTrace);
#else
        return DAsyncFlow.Create();
#endif
    }
}