using DTasks.Infrastructure.Marshaling;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IResumptionContext
{
    DAsyncId IResumptionContext.Id => _id;

    IDAsyncSurrogator IResumptionContext.Surrogator => this;
}