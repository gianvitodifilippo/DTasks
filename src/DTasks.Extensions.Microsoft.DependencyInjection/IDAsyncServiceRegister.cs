using System.Diagnostics.CodeAnalysis;

namespace DTasks.Extensions.Microsoft.DependencyInjection;

internal interface IDAsyncServiceRegister
{
    bool IsDAsyncService(Type serviceType);

    bool IsDAsyncService(ServiceTypeId id, [NotNullWhen(true)] out Type? serviceType);
}
