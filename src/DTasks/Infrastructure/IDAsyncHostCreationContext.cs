using DTasks.Infrastructure.Marshaling;

namespace DTasks.Infrastructure;

public interface IDAsyncHostCreationContext
{
    IDAsyncSurrogator Surrogator { get; }
}
