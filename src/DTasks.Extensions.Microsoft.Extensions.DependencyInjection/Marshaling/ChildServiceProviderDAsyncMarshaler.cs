using DTasks.Extensions.Microsoft.Extensions.DependencyInjection.Mapping;
using DTasks.Marshaling;

namespace DTasks.Extensions.Microsoft.Extensions.DependencyInjection.Marshaling;

internal class ChildServiceProviderDAsyncMarshaler(
    IServiceProvider provider,
    IDAsyncServiceRegister register,
    ITypeResolver typeResolver,
    IRootDAsyncMarshaler rootMarshaler) : ServiceProviderDAsyncMarshaler(provider, register, typeResolver), IRootDAsyncMarshaler, IChildServiceMapper
{
    protected override bool TryMarshal<T, TAction>(in T value, scoped ref TAction action)
    {
        return
            base.TryMarshal(in value, ref action) ||
            rootMarshaler.TryMarshal(in value, ref action);
    }

    public new void MapService(object service, ServiceToken token) => base.MapService(service, token);
}
