using System.ComponentModel;
using DTasks.Infrastructure.Execution;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class DAsyncHost : IDAsyncHost
{
    protected abstract IDAsyncStateManager StateManager { get; }
    
    protected virtual IDAsyncMarshaler Marshaler => DefaultDAsyncMarshaler.Instance;

    protected virtual IDAsyncTypeResolver TypeResolver => DAsyncTypeResolver.Default;

    protected virtual IDAsyncCancellationProvider CancellationProvider => DefaultDAsyncCancellationProvider.Instance;

    protected virtual IDAsyncSuspensionHandler SuspensionHandler => DefaultDAsyncSuspensionHandler.Instance;

    protected virtual Task OnStartAsync(IDAsyncResultBuilder resultBuilder, CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual Task OnSuspendAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual Task OnSucceedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual Task OnSucceedAsync<TResult>(TResult result, CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual Task OnFailAsync(Exception exception, CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual Task OnCancelAsync(OperationCanceledException exception, CancellationToken cancellationToken) => Task.CompletedTask;

    IDAsyncStateManager IDAsyncHost.StateManager => StateManager;

    IDAsyncMarshaler IDAsyncHost.Marshaler => Marshaler;

    IDAsyncTypeResolver IDAsyncHost.TypeResolver => TypeResolver;

    IDAsyncCancellationProvider IDAsyncHost.CancellationProvider => CancellationProvider;

    IDAsyncSuspensionHandler IDAsyncHost.SuspensionHandler => SuspensionHandler;
    
    Task IDAsyncHost.OnStartAsync(IDAsyncResultBuilder resultBuilder, CancellationToken cancellationToken) => OnStartAsync(resultBuilder, cancellationToken);

    Task IDAsyncHost.OnSuspendAsync(CancellationToken cancellationToken) => OnSuspendAsync(cancellationToken);

    Task IDAsyncHost.OnSucceedAsync(CancellationToken cancellationToken) => OnSucceedAsync(cancellationToken);

    Task IDAsyncHost.OnSucceedAsync<TResult>(TResult result, CancellationToken cancellationToken) => OnSucceedAsync(result, cancellationToken);

    Task IDAsyncHost.OnFailAsync(Exception exception, CancellationToken cancellationToken) => OnFailAsync(exception, cancellationToken);

    Task IDAsyncHost.OnCancelAsync(OperationCanceledException exception, CancellationToken cancellationToken) => OnCancelAsync(exception, cancellationToken);
}
