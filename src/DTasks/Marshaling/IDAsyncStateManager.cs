using DTasks.Infrastructure;
using System.ComponentModel;

namespace DTasks.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncStateManager
{
    ValueTask DehydrateAsync<TStateMachine>(DAsyncId parentId, DAsyncId id, ref TStateMachine stateMachine, ISuspensionContext suspensionContext, CancellationToken cancellationToken = default)
        where TStateMachine : notnull;

    ValueTask<DAsyncLink> HydrateAsync(DAsyncId id, CancellationToken cancellationToken = default);

    ValueTask<DAsyncLink> HydrateAsync<TResult>(DAsyncId id, TResult result, CancellationToken cancellationToken = default);

    ValueTask<DAsyncLink> HydrateAsync(DAsyncId id, Exception exception, CancellationToken cancellationToken = default);

    ValueTask<DAsyncId> DeleteAsync(DAsyncId id, CancellationToken cancellationToken = default);

    ValueTask FlushAsync(CancellationToken cancellationToken = default);
}
