using System.Collections.Frozen;

namespace DTasks.Extensions.Microsoft.DependencyInjection;

internal sealed class ServiceResolverBuilder : IServiceResolverBuilder
{
    private readonly Dictionary<ServiceTypeId, Type> _serviceTypes = [];
    private bool _isBuilt;

    public ServiceTypeId AddServiceType(Type serviceType)
    {
        EnsureNotBuilt();

        ServiceTypeId id = new(_serviceTypes.Count.ToString()); // TODO: We should use a more robust id generation strategy
        _serviceTypes.Add(id, serviceType);
        return id;
    }

    public ServiceResolver BuildServiceResolver()
    {
        EnsureNotBuilt();

        _isBuilt = true;
        return _serviceTypes.ToFrozenDictionary().TryGetValue;
    }

    private void EnsureNotBuilt()
    {
        if (_isBuilt)
            throw new InvalidOperationException("Service resolver was already built.");
    }
}
