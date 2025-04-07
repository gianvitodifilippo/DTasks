using System.ComponentModel;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Infrastructure.State;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncStack
{
    ValueTask DehydrateAsync<TStateMachine>(ISuspensionContext context, ref TStateMachine stateMachine, CancellationToken cancellationToken = default)
        where TStateMachine : notnull;

    ValueTask<DAsyncLink> HydrateAsync(IResumptionContext context, CancellationToken cancellationToken = default);

    ValueTask<DAsyncLink> HydrateAsync<TResult>(IResumptionContext context, TResult result, CancellationToken cancellationToken = default);

    ValueTask<DAsyncLink> HydrateAsync(IResumptionContext context, Exception exception, CancellationToken cancellationToken = default);

    ValueTask<DAsyncId> DeleteAsync(DAsyncId id, CancellationToken cancellationToken = default);

    ValueTask FlushAsync(CancellationToken cancellationToken = default);
}
