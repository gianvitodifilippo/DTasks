using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class DAsyncHost : IDAsyncHost, IDisposable
{
    protected virtual bool TryGetProperty<TProperty>(DAsyncPropertyKey<TProperty> key, [MaybeNullWhen(false)] out TProperty value)
    {
        value = default;
        return false;
    }
    
    bool IDAsyncHost.TryGetProperty<TProperty>(DAsyncPropertyKey<TProperty> key, [MaybeNullWhen(false)] out TProperty value) => TryGetProperty(key, out value);
    
    protected virtual void OnInitialize(IDAsyncFlowInitializationContext context) { }

    protected virtual void OnFinalize(IDAsyncFlowFinalizationContext context) { }

    protected virtual Task OnStartAsync(IDAsyncFlowStartContext context, CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual Task OnSuspendAsync(IDAsyncFlowSuspensionContext context, CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual Task OnSucceedAsync(IDAsyncFlowCompletionContext context, CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual Task OnSucceedAsync<TResult>(IDAsyncFlowCompletionContext context, TResult result, CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual Task OnFailAsync(IDAsyncFlowCompletionContext context, Exception exception, CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual Task OnCancelAsync(IDAsyncFlowCompletionContext context, OperationCanceledException exception, CancellationToken cancellationToken) => Task.CompletedTask;

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
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
