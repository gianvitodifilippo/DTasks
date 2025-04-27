using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace DTasks.Configuration.DependencyInjection;

[DebuggerDisplay("{ToString(),nq}")]
internal sealed class ScopedComponentDescriptor<TComponent>(FlowImplementationFactory<TComponent> createComponent) : IComponentDescriptor<TComponent>
    where TComponent : notnull
{
    public TReturn Build<TReturn>(IDAsyncInfrastructureBuilder<TComponent, TReturn> builder)
    {
        return builder.Scoped(createComponent);
    }
    
    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return $"Scoped({typeof(TComponent).Name}))";
    }
}