using System.Diagnostics.CodeAnalysis;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Extensions.DependencyInjection;

internal interface IDAsyncServiceRegister
{
    bool IsDAsyncService(Type serviceType);

    bool IsDAsyncService(TypeId typeId, [NotNullWhen(true)] out Type? serviceType);
}
