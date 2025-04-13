using DTasks.Extensions.DependencyInjection.Mapping;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Extensions.DependencyInjection.Marshaling;

internal sealed class RootServiceProviderDAsyncSurrogator(
    IServiceProvider provider,
    IDAsyncServiceRegister register,
    IDAsyncTypeResolver typeResolver) : ServiceProviderDAsyncSurrogator(provider, register, typeResolver), IRootDAsyncSurrogator, IRootServiceMapper
{
    public new void MapService(object service, ServiceSurrogate surrogate) => base.MapService(service, surrogate);
}
