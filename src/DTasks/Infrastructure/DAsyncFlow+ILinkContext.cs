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

        if (_handleBuilder is null)
            throw new InvalidOperationException("SetResult/SetException was already called.");
        
        IHandleBuilder handleBuilder = ConsumeNotNull(ref _handleBuilder);
        handleBuilder.SetResult(this);
    }

    void ILinkContext.SetResult<TResult>(TResult result)
    {
        AssertState<IHydrationContext>(FlowState.Linking);
        
        if (_handleBuilder is null)
            throw new InvalidOperationException("SetResult/SetException was already called.");

        IHandleBuilder handleBuilder = ConsumeNotNull(ref _handleBuilder);
        handleBuilder.SetResult(this, result);
    }

    void ILinkContext.SetException(Exception exception)
    {
        AssertState<IHydrationContext>(FlowState.Linking);
        
        if (_handleBuilder is null)
            throw new InvalidOperationException("SetResult/SetException was already called.");

        IHandleBuilder handleBuilder = ConsumeNotNull(ref _handleBuilder);
        handleBuilder.SetException(this, exception);
    }
}