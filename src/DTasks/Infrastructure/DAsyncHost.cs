using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using DTasks.Configuration;
using DTasks.Utils;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class DAsyncHost : IDAsyncHost, IDisposable
{
    private DAsyncRunner? _runner = DAsyncRunner.Create();

    protected abstract DTasksConfiguration Configuration { get; }

    public ValueTask StartAsync(IDAsyncRunnable runnable, CancellationToken cancellationToken = default)
    {
        CheckDisposed();

        return _runner.StartAsync(this, runnable, cancellationToken);
    }

    public ValueTask ResumeAsync(DAsyncId id, CancellationToken cancellationToken = default)
    {
        CheckDisposed();

        return _runner.ResumeAsync(this, id, cancellationToken);
    }

    public ValueTask ResumeAsync<TResult>(DAsyncId id, TResult result, CancellationToken cancellationToken = default)
    {
        CheckDisposed();

        return _runner.ResumeAsync(this, id, result, cancellationToken);
    }

    public ValueTask ResumeAsync(DAsyncId id, Exception exception, CancellationToken cancellationToken = default)
    {
        CheckDisposed();

        return _runner.ResumeAsync(this, id, exception, cancellationToken);
    }

    protected virtual void OnInitialize(IDAsyncFlowInitializationContext context) { }

    protected virtual void OnFinalize(IDAsyncFlowFinalizationContext context) { }

    protected virtual Task OnStartAsync(IDAsyncFlowStartContext context, CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual Task OnSuspendAsync(IDAsyncFlowSuspensionContext context, CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual Task OnSucceedAsync(IDAsyncFlowCompletionContext context, CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual Task OnSucceedAsync<TResult>(IDAsyncFlowCompletionContext context, TResult result, CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual Task OnFailAsync(IDAsyncFlowCompletionContext context, Exception exception, CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual Task OnCancelAsync(IDAsyncFlowCompletionContext context, OperationCanceledException exception, CancellationToken cancellationToken) => Task.CompletedTask;

    DTasksConfiguration IDAsyncHost.Configuration => Configuration;

    void IDAsyncHost.OnInitialize(IDAsyncFlowInitializationContext context) => OnInitialize(context);

    void IDAsyncHost.OnFinalize(IDAsyncFlowFinalizationContext context) => OnFinalize(context);

    Task IDAsyncHost.OnStartAsync(IDAsyncFlowStartContext context, CancellationToken cancellationToken) => OnStartAsync(context, cancellationToken);

    Task IDAsyncHost.OnSuspendAsync(IDAsyncFlowSuspensionContext context, CancellationToken cancellationToken) => OnSuspendAsync(context, cancellationToken);

    Task IDAsyncHost.OnSucceedAsync(IDAsyncFlowCompletionContext context, CancellationToken cancellationToken) => OnSucceedAsync(context, cancellationToken);

    Task IDAsyncHost.OnSucceedAsync<TResult>(IDAsyncFlowCompletionContext context, TResult result, CancellationToken cancellationToken) => OnSucceedAsync(context, result, cancellationToken);

    Task IDAsyncHost.OnFailAsync(IDAsyncFlowCompletionContext context, Exception exception, CancellationToken cancellationToken) => OnFailAsync(context, exception, cancellationToken);

    Task IDAsyncHost.OnCancelAsync(IDAsyncFlowCompletionContext context, OperationCanceledException exception, CancellationToken cancellationToken) => OnCancelAsync(context, exception, cancellationToken);

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        _runner?.Dispose();
        _runner = null;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    [MemberNotNull(nameof(_runner))]
    private void CheckDisposed()
    {
        if (_runner is null)
            throw new ObjectDisposedException(GetType().Name);
    }

    public static DAsyncHost CreateDefault()
    {
        return new DefaultDAsyncHost(DTasksConfiguration.Create());
    }

    public static DAsyncHost CreateDefault(Action<IDTasksConfigurationBuilder> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        return new DefaultDAsyncHost(DTasksConfiguration.Create(configure));
    }

    public static DAsyncHost CreateDefault(DTasksConfiguration configuration)
    {
        ThrowHelper.ThrowIfNull(configuration);

        return new DefaultDAsyncHost(configuration);
    }

    private sealed class DefaultDAsyncHost(DTasksConfiguration configuration) : DAsyncHost
    {
        protected override DTasksConfiguration Configuration => configuration;
    }
}
