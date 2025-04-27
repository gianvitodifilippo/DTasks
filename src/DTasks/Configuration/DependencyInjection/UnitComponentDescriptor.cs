using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace DTasks.Configuration.DependencyInjection;

[DebuggerDisplay("{ToString(),nq}")]
internal sealed class UnitComponentDescriptor<TComponent>(TComponent component) : IComponentDescriptor<TComponent>
    where TComponent : notnull
{
    public TReturn Build<TReturn>(IDAsyncInfrastructureBuilder<TComponent, TReturn> builder)
    {
        return builder.Unit(component);
    }
    
    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return $"Unit({typeof(TComponent).Name}))";
    }
}
