using System.ComponentModel;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Infrastructure.State;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncStack
{
    // TODO: See if we can pass parentId and id within suspension context

    ValueTask DehydrateAsync<TStateMachine>(ISuspensionContext context, DAsyncId parentId, DAsyncId id, ref TStateMachine stateMachine, CancellationToken cancellationToken = default)
        where TStateMachine : notnull;

    ValueTask<DAsyncLink> HydrateAsync(IResumptionContext context, DAsyncId id, CancellationToken cancellationToken = default);

    ValueTask<DAsyncLink> HydrateAsync<TResult>(IResumptionContext context, DAsyncId id, TResult result, CancellationToken cancellationToken = default);

    ValueTask<DAsyncLink> HydrateAsync(IResumptionContext context, DAsyncId id, Exception exception, CancellationToken cancellationToken = default);

    ValueTask<DAsyncId> DeleteAsync(DAsyncId id, CancellationToken cancellationToken = default);

    ValueTask FlushAsync(CancellationToken cancellationToken = default);
}
