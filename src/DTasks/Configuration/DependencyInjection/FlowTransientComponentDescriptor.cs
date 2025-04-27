using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace DTasks.Configuration.DependencyInjection;

[DebuggerDisplay("{ToString(),nq}")]
internal sealed class FlowTransientComponentDescriptor<TComponent>(FlowImplementationFactory<TComponent> createComponent) : IComponentDescriptor<TComponent>
    where TComponent : notnull
{
    public TReturn Build<TReturn>(IDAsyncInfrastructureBuilder<TComponent, TReturn> builder)
    {
        return builder.FlowTransient(createComponent);
    }
    
    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return $"FlowTransient({typeof(TComponent).Name}))";
    }
}
