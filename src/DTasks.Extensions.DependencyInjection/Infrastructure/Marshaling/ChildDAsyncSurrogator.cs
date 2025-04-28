using DTasks.Infrastructure.Marshaling;

namespace DTasks.Extensions.DependencyInjection.Infrastructure.Marshaling;

internal class ChildDAsyncSurrogator(
    IServiceProvider provider,
    IDAsyncServiceRegister register,
    IDAsyncTypeResolver typeResolver,
    IRootDAsyncSurrogator rootSurrogator) : ServiceProviderDAsyncSurrogator(provider, register, typeResolver), IChildServiceMapper
{
    protected override bool TrySurrogate<T, TAction>(in T value, scoped ref TAction action)
    {
        return
            base.TrySurrogate(in value, ref action) ||
            rootSurrogator.TrySurrogate(in value, ref action);
    }

    public new void MapService(object service, ServiceSurrogate surrogate) => base.MapService(service, surrogate);
}
