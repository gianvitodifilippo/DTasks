namespace DTasks.Hosting;

internal partial class DAsyncFlow
{
    private void Hydrate(DAsyncId id)
    {
        _state = FlowState.Hydrating;

        try
        {
            Await(_stateManager.HydrateAsync(id, _cancellationToken));
        }
        catch (Exception ex)
        {
            _valueTaskSource.SetException(ex);
        }
    }

    private void Hydrate<TResult>(DAsyncId id, TResult result)
    {
        _state = FlowState.Hydrating;

        try
        {
            Await(_stateManager.HydrateAsync(id, result, _cancellationToken));
        }
        catch (Exception ex)
        {
            _valueTaskSource.SetException(ex);
        }
    }

    private void Hydrate(DAsyncId id, Exception exception)
    {
        _state = FlowState.Hydrating;

        try
        {
            Await(_stateManager.HydrateAsync(id, exception, _cancellationToken));
        }
        catch (Exception ex)
        {
            _valueTaskSource.SetException(ex);
        }
    }

    private void Dehydrate<TStateMachine>(DAsyncId parentId, DAsyncId id, ref TStateMachine stateMachine)
        where TStateMachine : notnull
    {
        _state = FlowState.Dehydrating;

        try
        {
            Await(_stateManager.DehydrateAsync(parentId, id, ref stateMachine, this, _cancellationToken));
        }
        catch (Exception ex)
        {
            _continuation = null;
            _suspendingAwaiterOrType = null;
            _valueTaskSource.SetException(ex);
        }
    }
}
