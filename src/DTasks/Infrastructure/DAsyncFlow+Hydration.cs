namespace DTasks.Infrastructure;

internal partial class DAsyncFlow
{
    private void Hydrate(DAsyncId id)
    {
        Await(_stateManager.HydrateAsync(id, _cancellationToken), FlowState.Hydrating);
    }

    private void Hydrate<TResult>(DAsyncId id, TResult result)
    {
        Await(_stateManager.HydrateAsync(id, result, _cancellationToken), FlowState.Hydrating);
    }

    private void Hydrate(DAsyncId id, Exception exception)
    {
        Await(_stateManager.HydrateAsync(id, exception, _cancellationToken), FlowState.Hydrating);
    }

    private void Dehydrate<TStateMachine>(DAsyncId parentId, DAsyncId id, ref TStateMachine stateMachine)
        where TStateMachine : notnull
    {
        try
        {
            Await(_stateManager.DehydrateAsync(parentId, id, ref stateMachine, this, _cancellationToken), FlowState.Dehydrating);
        }
        catch
        {
            _continuation = null;
            _suspendingAwaiterOrType = null;
            throw;
        }
    }
}
