using DTasks.Infrastructure.State;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IHydrationContext
{
    DAsyncId IHydrationContext.Id
    {
        get
        {
            AssertState<IHydrationContext>(FlowState.Hydrating);

            return _id;
        }
    }
}