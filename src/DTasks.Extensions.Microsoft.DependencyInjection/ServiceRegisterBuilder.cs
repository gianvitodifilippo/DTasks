using System.Collections.Frozen;
using System.Diagnostics;

namespace DTasks.Extensions.Microsoft.DependencyInjection;

internal sealed class ServiceRegisterBuilder : IServiceRegisterBuilder
{
    private readonly HashSet<Type> _types = [];
    private readonly Dictionary<ServiceTypeId, Type> _idsToTypes = [];

    public ServiceTypeId AddServiceType(Type serviceType)
    {
        Debug.Assert(!_types.Contains(serviceType), $"'{serviceType.Name}' was already registered as a d-async service.");

        ServiceTypeId id = new(_idsToTypes.Count.ToString()); // TODO: We should use a more robust id generation strategy

        _types.Add(serviceType);
        _idsToTypes.Add(id, serviceType);

        return id;
    }

    public IServiceRegister Build()
    {
        return new ServiceRegister(_types.ToFrozenSet(), _idsToTypes.ToFrozenDictionary());
    }
}
