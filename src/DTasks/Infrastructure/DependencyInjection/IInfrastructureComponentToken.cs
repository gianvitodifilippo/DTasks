using DTasks.Configuration.DependencyInjection;

namespace DTasks.Infrastructure.DependencyInjection;

internal interface IInfrastructureComponentToken<out TComponent> : IComponentToken<TComponent>
{
    TComponent CreateComponent(ComponentProvider provider);
}