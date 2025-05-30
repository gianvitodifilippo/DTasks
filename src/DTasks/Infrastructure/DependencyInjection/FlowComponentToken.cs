using DTasks.Configuration.DependencyInjection;

namespace DTasks.Infrastructure.DependencyInjection;

internal sealed class FlowComponentToken<TComponent>(
    Func<IComponentProvider, TComponent> createComponent,
    bool transient) : InfrastructureComponentToken<TComponent>
{
    public override TComponent CreateComponent(ComponentProvider provider)
    {
        return provider.GetFlowComponent(this, createComponent, transient);
    }

    public override InfrastructureComponentToken<TComponent> AsRoot()
    {
        return new FlowComponentToken<TComponent>(createComponent, transient);
    }

    public override InfrastructureComponentToken<TComponent> AsHost()
    {
        return new FlowComponentToken<TComponent>(createComponent, transient);
    }

    public override InfrastructureComponentToken<TComponent> AsFlow()
    {
        return new FlowComponentToken<TComponent>(createComponent, transient);
    }

    public override InfrastructureComponentToken<TResult> Bind<TResult>(IComponentProviderBuilder builder, IComponentDescriptor<TResult> resolvedResultDescriptor)
    {
        return builder
            .GetTokenInFlowScope(resolvedResultDescriptor)
            .AsFlow();
    }
}
