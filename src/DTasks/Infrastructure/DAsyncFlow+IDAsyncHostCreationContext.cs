using DTasks.Infrastructure.Marshaling;

namespace DTasks.Infrastructure;

public sealed partial class DAsyncFlow : IDAsyncHostCreationContext
{
    IDAsyncSurrogator IDAsyncHostCreationContext.Surrogator => this;
}
