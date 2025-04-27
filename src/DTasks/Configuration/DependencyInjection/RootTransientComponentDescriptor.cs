using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace DTasks.Configuration.DependencyInjection;

[DebuggerDisplay("{ToString(),nq}")]
internal sealed class RootTransientComponentDescriptor<TComponent>(ConfiguredImplementationFactory<TComponent> createComponent) : IComponentDescriptor<TComponent>
    where TComponent : notnull
{
    public TReturn Build<TReturn>(IDAsyncInfrastructureBuilder<TComponent, TReturn> builder)
    {
        return builder.RootTransient(createComponent);
    }
    
    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return $"RootTransient({typeof(TComponent).Name}))";
    }
}
