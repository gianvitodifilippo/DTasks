using DTasks.Configuration.DependencyInjection;

namespace DTasks.Infrastructure.DependencyInjection;

internal sealed class FlowScopeComponentToken : InfrastructureComponentToken<IDAsyncFlowScope>
{
    public static readonly FlowScopeComponentToken Instance = new();
    
    private FlowScopeComponentToken()
    {
    }

    public override IDAsyncFlowScope CreateComponent(ComponentProvider provider)
    {
        return provider.FlowScope;
    }

    public override InfrastructureComponentToken<IDAsyncFlowScope> AsRoot()
    {
        return new FlowScopeComponentToken();
    }

    public override InfrastructureComponentToken<IDAsyncFlowScope> AsHost()
    {
        return new FlowScopeComponentToken();
    }

    public override InfrastructureComponentToken<IDAsyncFlowScope> AsFlow()
    {
        return new FlowScopeComponentToken();
    }

    public override InfrastructureComponentToken<TResult> Bind<TResult>(IComponentProviderBuilder builder, IComponentDescriptor<TResult> resolvedResultDescriptor)
    {
        return builder
            .GetTokenInFlowScope(resolvedResultDescriptor)
            .AsFlow();
    }
}