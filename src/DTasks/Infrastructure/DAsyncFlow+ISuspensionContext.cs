using DTasks.Infrastructure.Marshaling;
using DTasks.Utils;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : ISuspensionContext
{
    DAsyncId ISuspensionContext.Id
    {
        get
        {
            AssertState<ISuspensionContext>(FlowState.Dehydrating);
            
            return _id;
        }
    }
    
    DAsyncId ISuspensionContext.ParentId
    {
        get
        {
            AssertState<ISuspensionContext>(FlowState.Dehydrating);
            
            return _parentId;
        }
    }
    
    bool ISuspensionContext.IsSuspended<TAwaiter>(ref TAwaiter awaiter)
    {
        AssertState<ISuspensionContext>(FlowState.Dehydrating);
        Assert.NotNull(_suspendingAwaiterOrType);
        
        return typeof(TAwaiter).IsValueType
            ? _suspendingAwaiterOrType is Type type && type == typeof(TAwaiter)
            : ReferenceEquals(_suspendingAwaiterOrType, awaiter);
    }
}
