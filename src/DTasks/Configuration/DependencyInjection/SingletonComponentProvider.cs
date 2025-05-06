using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace DTasks.Configuration.DependencyInjection;

[DebuggerDisplay("{ToString(),nq}")]
internal sealed class SingletonComponentProvider<TComponent>(
    DTasksConfiguration configuration,
    TComponent component) : RootComponentProvider<TComponent>
    where TComponent : notnull
{
    public TComponent Component => component;
    
    protected override DTasksConfiguration Configuration => configuration;

    protected override TComponent GetComponent() => component;

    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return $"Singleton({typeof(TComponent).Name}))";
    }
}
