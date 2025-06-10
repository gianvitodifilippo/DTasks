using DTasks.Infrastructure.State;
using DTasks.Utils;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDehydrationContext
{
    DAsyncId IDehydrationContext.Id
    {
        get
        {
            AssertState<IDehydrationContext>(FlowState.Dehydrating);

            return _id;
        }
    }
    
    DAsyncId IDehydrationContext.ParentId
    {
        get
        {
            AssertState<IDehydrationContext>(FlowState.Dehydrating);

            return _parentId;
        }
    }
    
    bool IDehydrationContext.IsSuspended<TAwaiter>(ref TAwaiter awaiter)
    {
        AssertState<IDehydrationContext>(FlowState.Dehydrating);
        Assert.NotNull(_suspendingAwaiterOrType);
        
        return typeof(TAwaiter).IsValueType
            ? _suspendingAwaiterOrType is Type type && type == typeof(TAwaiter)
            : ReferenceEquals(_suspendingAwaiterOrType, awaiter);
    }
}
