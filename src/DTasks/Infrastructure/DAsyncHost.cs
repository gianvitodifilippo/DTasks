using System.ComponentModel;
using DTasks.Infrastructure.Execution;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class DAsyncHost : IDAsyncHost
{
    protected abstract IDAsyncStateManager StateManager { get; }
    
    protected virtual IDAsyncSurrogator Surrogator => DefaultDAsyncSurrogator.Instance;

    protected virtual IDAsyncTypeResolver TypeResolver => DAsyncTypeResolver.Default;

    protected virtual IDAsyncCancellationProvider CancellationProvider => DefaultDAsyncCancellationProvider.Instance;

    protected virtual IDAsyncSuspensionHandler SuspensionHandler => DefaultDAsyncSuspensionHandler.Instance;

    protected virtual Task OnStartAsync(IDAsyncFlowStartContext context, CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual Task OnSuspendAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual Task OnSucceedAsync(IDAsyncFlowCompletionContext context, CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual Task OnSucceedAsync<TResult>(IDAsyncFlowCompletionContext context, TResult result, CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual Task OnFailAsync(IDAsyncFlowCompletionContext context, Exception exception, CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual Task OnCancelAsync(IDAsyncFlowCompletionContext context, OperationCanceledException exception, CancellationToken cancellationToken) => Task.CompletedTask;

    IDAsyncStateManager IDAsyncHost.StateManager => StateManager;

    IDAsyncSurrogator IDAsyncHost.Surrogator => Surrogator;

    IDAsyncTypeResolver IDAsyncHost.TypeResolver => TypeResolver;

    IDAsyncCancellationProvider IDAsyncHost.CancellationProvider => CancellationProvider;

    IDAsyncSuspensionHandler IDAsyncHost.SuspensionHandler => SuspensionHandler;
    
    Task IDAsyncHost.OnStartAsync(IDAsyncFlowStartContext context, CancellationToken cancellationToken) => OnStartAsync(context, cancellationToken);

    Task IDAsyncHost.OnSuspendAsync(CancellationToken cancellationToken) => OnSuspendAsync(cancellationToken);

    Task IDAsyncHost.OnSucceedAsync(IDAsyncFlowCompletionContext context, CancellationToken cancellationToken) => OnSucceedAsync(context, cancellationToken);

    Task IDAsyncHost.OnSucceedAsync<TResult>(IDAsyncFlowCompletionContext context, TResult result, CancellationToken cancellationToken) => OnSucceedAsync(context, result, cancellationToken);

    Task IDAsyncHost.OnFailAsync(IDAsyncFlowCompletionContext context, Exception exception, CancellationToken cancellationToken) => OnFailAsync(context, exception, cancellationToken);

    Task IDAsyncHost.OnCancelAsync(IDAsyncFlowCompletionContext context, OperationCanceledException exception, CancellationToken cancellationToken) => OnCancelAsync(context, exception, cancellationToken);
}
