using System.Collections.Frozen;
using System.Diagnostics;

namespace DTasks.Extensions.Microsoft.DependencyInjection;

internal sealed class DAsyncServiceRegisterBuilder : IDAsyncServiceRegisterBuilder
{
    private readonly HashSet<Type> _types = [];
    private readonly Dictionary<ServiceTypeId, Type> _idsToTypes = [];

    public ServiceTypeId AddServiceType(Type serviceType)
    {
        Debug.Assert(!_types.Contains(serviceType), $"'{serviceType.Name}' was already registered as a d-async service.");

        ServiceTypeId id = new(_idsToTypes.Count.ToString());

        _types.Add(serviceType);
        _idsToTypes.Add(id, serviceType);

        return id;
    }

    public IDAsyncServiceRegister Build()
    {
        return new DAsyncServiceRegister(_types.ToFrozenSet(), _idsToTypes.ToFrozenDictionary());
    }
}
