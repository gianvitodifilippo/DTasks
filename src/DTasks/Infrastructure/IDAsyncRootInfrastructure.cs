using DTasks.Infrastructure.Marshaling;

namespace DTasks.Infrastructure;

public interface IDAsyncRootInfrastructure
{
    IDAsyncTypeResolver TypeResolver { get; }
}