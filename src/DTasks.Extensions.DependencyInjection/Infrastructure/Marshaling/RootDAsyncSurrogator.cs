using DTasks.Infrastructure.Marshaling;

namespace DTasks.Extensions.DependencyInjection.Infrastructure.Marshaling;

internal sealed class RootDAsyncSurrogator(
    IServiceProvider provider,
    IDAsyncServiceRegister register,
    IDAsyncTypeResolver typeResolver) : ServiceProviderDAsyncSurrogator(provider, register, typeResolver), IRootDAsyncSurrogator, IRootServiceMapper
{
    public new void MapService(object service, ServiceSurrogate surrogate) => base.MapService(service, surrogate);
}
