using System.ComponentModel;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Infrastructure.State;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncStack
{
    ValueTask DehydrateAsync<TStateMachine>(IDehydrationContext context, ref TStateMachine stateMachine, CancellationToken cancellationToken = default)
        where TStateMachine : notnull;
    
    ValueTask DehydrateCompletedAsync(DAsyncId id, CancellationToken cancellationToken = default);
    
    ValueTask DehydrateCompletedAsync<TResult>(DAsyncId id, TResult result, CancellationToken cancellationToken = default);

    ValueTask DehydrateCompletedAsync(DAsyncId id, Exception exception, CancellationToken cancellationToken = default);
    
    ValueTask<DAsyncLink> HydrateAsync(IHydrationContext context, CancellationToken cancellationToken = default);

    ValueTask<DAsyncLink> HydrateAsync<TResult>(IHydrationContext context, TResult result, CancellationToken cancellationToken = default);

    ValueTask<DAsyncLink> HydrateAsync(IHydrationContext context, Exception exception, CancellationToken cancellationToken = default);

    ValueTask LinkAsync(ILinkContext context, CancellationToken cancellationToken = default);
    
    ValueTask FlushAsync(CancellationToken cancellationToken = default);
}
