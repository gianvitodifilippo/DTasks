using DTasks.Configuration;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncFlowScope
{
    IDAsyncSurrogator IDAsyncFlowScope.Surrogator => this;
}
