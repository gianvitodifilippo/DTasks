using DTasks.Infrastructure.Marshaling;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IResumptionContext
{
    DAsyncId IResumptionContext.Id
    {
        get
        {
            AssertState<IResumptionContext>(FlowState.Hydrating);

            return _id;
        }
    }
}