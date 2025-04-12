using DTasks.Extensions.DependencyInjection.Mapping;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Extensions.DependencyInjection.Marshaling;

internal sealed class RootServiceProviderDAsyncMarshaler(
    IServiceProvider provider,
    IDAsyncServiceRegister register,
    IDAsyncTypeResolver typeResolver) : ServiceProviderDAsyncMarshaler(provider, register, typeResolver), IRootDAsyncMarshaler, IRootServiceMapper
{
    public new void MapService(object service, ServiceToken token) => base.MapService(service, token);
}
