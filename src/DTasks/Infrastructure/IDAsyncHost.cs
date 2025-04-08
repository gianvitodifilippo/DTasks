using System.ComponentModel;
using DTasks.Execution;
using DTasks.Infrastructure.Execution;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncHost
{
    IDAsyncStateManager StateManager { get; }

    IDAsyncMarshaler Marshaler { get; }
    
    IDAsyncTypeResolver TypeResolver { get; }
    
    IDAsyncCancellationProvider CancellationProvider { get; }
    
    IDAsyncSuspensionHandler SuspensionHandler { get; }
    
    // TODO: Add distributed lock provider
    
    Task OnStartAsync(IDAsyncResultBuilder resultBuilder, CancellationToken cancellationToken);
    
    Task OnSuspendAsync(CancellationToken cancellationToken);

    Task OnSucceedAsync(CancellationToken cancellationToken);

    Task OnSucceedAsync<TResult>(TResult result, CancellationToken cancellationToken);

    Task OnFailAsync(Exception exception, CancellationToken cancellationToken);

    Task OnCancelAsync(OperationCanceledException exception, CancellationToken cancellationToken);
}