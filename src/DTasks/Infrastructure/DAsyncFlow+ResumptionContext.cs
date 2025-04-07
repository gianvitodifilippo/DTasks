using DTasks.Infrastructure.Marshaling;

namespace DTasks.Infrastructure;

public sealed partial class DAsyncFlow : IResumptionContext
{
    DAsyncId IResumptionContext.Id => _id;

    IDAsyncMarshaler IResumptionContext.Marshaler => this;
}