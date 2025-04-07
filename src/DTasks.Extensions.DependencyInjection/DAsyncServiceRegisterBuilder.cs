using System.Collections.Frozen;
using System.Diagnostics;

namespace DTasks.Extensions.DependencyInjection;

internal sealed class DAsyncServiceRegisterBuilder(ITypeResolverBuilder typeResolverBuilder) : IDAsyncServiceRegisterBuilder
{
    private readonly HashSet<Type> _types = [];

    public TypeId AddServiceType(Type serviceType)
    {
        Debug.Assert(!_types.Contains(serviceType), $"'{serviceType.Name}' was already registered as a d-async service.");

        TypeId typeId = typeResolverBuilder.Register(serviceType);
        _types.Add(serviceType);

        return typeId;
    }

    public IDAsyncServiceRegister Build(IDAsyncTypeResolver typeResolver)
    {
        return new DAsyncServiceRegister(_types.ToFrozenSet(), typeResolver);
    }
}
