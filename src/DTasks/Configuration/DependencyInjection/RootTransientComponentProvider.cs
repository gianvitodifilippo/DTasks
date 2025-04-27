using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace DTasks.Configuration.DependencyInjection;

[DebuggerDisplay("{ToString(),nq}")]
internal sealed class RootTransientComponentProvider<TComponent>(
    DTasksConfiguration configuration,
    ConfiguredImplementationFactory<TComponent> createComponent) : RootComponentProvider<TComponent>
    where TComponent : notnull
{
    protected override DTasksConfiguration Configuration => configuration;

    protected override TComponent GetComponent() => createComponent(configuration);
    
    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return $"RootTransient({typeof(TComponent).Name}))";
    }
}
