using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace DTasks.Extensions.Microsoft.DependencyInjection;

internal class ServiceRegister(FrozenSet<Type> types, FrozenDictionary<ServiceTypeId, Type> idsToTypes) : IServiceRegister
{
    public bool IsDAsyncService(Type serviceType) => types.Contains(serviceType);

    public bool IsDAsyncService(ServiceTypeId id, [NotNullWhen(true)] out Type? serviceType) => idsToTypes.TryGetValue(id, out serviceType);
}
