using DTasks.Configuration.DependencyInjection;

namespace DTasks.Infrastructure.DependencyInjection;

internal abstract class InfrastructureComponentToken<TComponent> : IInfrastructureComponentToken<TComponent>
{
    public abstract TComponent CreateComponent(ComponentProvider provider);

    public abstract InfrastructureComponentToken<TResult> Bind<TResult>(
        IComponentProviderBuilder builder,
        IComponentDescriptor<TResult> resolvedResultDescriptor);
}