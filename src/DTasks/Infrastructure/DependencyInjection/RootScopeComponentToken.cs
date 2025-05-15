using DTasks.Configuration.DependencyInjection;

namespace DTasks.Infrastructure.DependencyInjection;

internal sealed class RootScopeComponentToken : InfrastructureComponentToken<IDAsyncRootScope>
{
    public static readonly RootScopeComponentToken Instance = new();
    
    private RootScopeComponentToken()
    {
    }
    
    public override IDAsyncRootScope CreateComponent(ComponentProvider provider)
    {
        return provider.RootScope;
    }

    public override InfrastructureComponentToken<TResult> Bind<TResult>(IComponentProviderBuilder builder, IComponentDescriptor<TResult> resolvedResultDescriptor)
    {
        return builder.GetTokenInRootScope(resolvedResultDescriptor);
    }
}