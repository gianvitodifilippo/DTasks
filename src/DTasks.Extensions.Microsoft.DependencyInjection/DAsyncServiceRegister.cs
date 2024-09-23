using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace DTasks.Extensions.Microsoft.DependencyInjection;

internal class DAsyncServiceRegister(FrozenSet<Type> types, FrozenDictionary<ServiceTypeId, Type> idsToTypes) : IDAsyncServiceRegister
{
    public bool IsDAsyncService(Type serviceType) => types.Contains(serviceType);

    public bool IsDAsyncService(ServiceTypeId id, [NotNullWhen(true)] out Type? serviceType) => idsToTypes.TryGetValue(id, out serviceType);
}
