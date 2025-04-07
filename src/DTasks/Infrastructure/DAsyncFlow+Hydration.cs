namespace DTasks.Infrastructure;

public sealed partial class DAsyncFlow
{
    private void Hydrate(DAsyncId id)
    {
        Await(_host.StateManager.Stack.HydrateAsync(this, _cancellationToken), FlowState.Hydrating);
    }

    private void Hydrate<TResult>(DAsyncId id, TResult result)
    {
        Await(_host.StateManager.Stack.HydrateAsync(this, result, _cancellationToken), FlowState.Hydrating);
    }

    private void Hydrate(DAsyncId id, Exception exception)
    {
        Await(_host.StateManager.Stack.HydrateAsync(this, exception, _cancellationToken), FlowState.Hydrating);
    }

    private void Dehydrate<TStateMachine>(DAsyncId parentId, DAsyncId id, ref TStateMachine stateMachine)
        where TStateMachine : notnull
    {
        try
        {
            Await(_host.StateManager.Stack.DehydrateAsync(this, ref stateMachine, _cancellationToken), FlowState.Dehydrating);
        }
        catch
        {
            _continuation = null;
            _suspendingAwaiterOrType = null;
            throw;
        }
    }
}
