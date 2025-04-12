using System.ComponentModel;
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
    
    Task OnStartAsync(IDAsyncFlowStartContext context, CancellationToken cancellationToken);
    
    Task OnSuspendAsync(CancellationToken cancellationToken);

    Task OnSucceedAsync(IDAsyncFlowCompletionContext context, CancellationToken cancellationToken);

    Task OnSucceedAsync<TResult>(IDAsyncFlowCompletionContext context, TResult result, CancellationToken cancellationToken);

    Task OnFailAsync(IDAsyncFlowCompletionContext context, Exception exception, CancellationToken cancellationToken);

    Task OnCancelAsync(IDAsyncFlowCompletionContext context, OperationCanceledException exception, CancellationToken cancellationToken);
}