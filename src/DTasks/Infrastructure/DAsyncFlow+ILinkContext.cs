using DTasks.Infrastructure.State;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : ILinkContext
{
    DAsyncId ILinkContext.Id
    {
        get
        {
            AssertState<IHydrationContext>(FlowState.Linking);

            return _handleId;
        }
    }

    DAsyncId ILinkContext.ParentId
    {
        get
        {
            AssertState<IHydrationContext>(FlowState.Linking);

            return _id;
        }
    }

    void ILinkContext.SetResult()
    {
        AssertState<IHydrationContext>(FlowState.Linking);

        if (_handleResultHandler is null)
            throw new InvalidOperationException("SetResult/SetException was already called.");
        
        IHandleResultHandler resultHandler = ConsumeNotNull(ref _handleResultHandler);
        resultHandler.SetResult(this);
    }

    void ILinkContext.SetResult<TResult>(TResult result)
    {
        AssertState<IHydrationContext>(FlowState.Linking);
        
        if (_handleResultHandler is null)
            throw new InvalidOperationException("SetResult/SetException was already called.");

        IHandleResultHandler resultHandler = ConsumeNotNull(ref _handleResultHandler);
        resultHandler.SetResult(this, result);
    }

    void ILinkContext.SetException(Exception exception)
    {
        AssertState<IHydrationContext>(FlowState.Linking);
        
        if (_handleResultHandler is null)
            throw new InvalidOperationException("SetResult/SetException was already called.");

        IHandleResultHandler resultHandler = ConsumeNotNull(ref _handleResultHandler);
        resultHandler.SetException(this, exception);
    }
}