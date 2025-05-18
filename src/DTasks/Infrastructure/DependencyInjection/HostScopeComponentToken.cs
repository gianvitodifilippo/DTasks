using DTasks.Configuration.DependencyInjection;

namespace DTasks.Infrastructure.DependencyInjection;

internal sealed class HostScopeComponentToken : InfrastructureComponentToken<IDAsyncHostScope>
{
    public static readonly HostScopeComponentToken Instance = new();
    
    private HostScopeComponentToken()
    {
    }
    
    public override IDAsyncHostScope CreateComponent(ComponentProvider provider)
    {
        return provider.HostScope;
    }

    public override InfrastructureComponentToken<IDAsyncHostScope> AsRoot()
    {
        return new HostScopeComponentToken();
    }

    public override InfrastructureComponentToken<IDAsyncHostScope> AsHost()
    {
        return new HostScopeComponentToken();
    }

    public override InfrastructureComponentToken<IDAsyncHostScope> AsFlow()
    {
        return new HostScopeComponentToken();
    }

    public override InfrastructureComponentToken<TResult> Bind<TResult>(IComponentProviderBuilder builder, IComponentDescriptor<TResult> resolvedResultDescriptor)
    {
        return builder
            .GetTokenInHostScope(resolvedResultDescriptor)
            .AsHost();
    }
}