using DTasks.Configuration.DependencyInjection;

namespace DTasks.Infrastructure.DependencyInjection;

internal sealed class RootComponentToken<TComponent>(
    Func<IComponentProvider, TComponent> createComponent,
    bool transient) : InfrastructureComponentToken<TComponent>
{
    public override TComponent CreateComponent(ComponentProvider provider)
    {
        return provider.GetRootComponent(this, createComponent, transient);
    }

    public override InfrastructureComponentToken<TResult> Bind<TResult>(IComponentProviderBuilder builder, IComponentDescriptor<TResult> resolvedResultDescriptor)
    {
        return builder.GetTokenInRootScope(resolvedResultDescriptor);
    }
}
