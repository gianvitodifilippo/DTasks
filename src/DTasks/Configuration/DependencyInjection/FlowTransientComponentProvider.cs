using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using DTasks.Infrastructure;

namespace DTasks.Configuration.DependencyInjection;

[DebuggerDisplay("{ToString(),nq}")]
internal sealed class FlowTransientComponentProvider<TComponent>(FlowImplementationFactory<TComponent> createComponent) : FlowComponentProvider<TComponent>
    where TComponent : notnull
{
    public override TComponent GetComponent(IDAsyncScope scope)
    {
        return createComponent(scope);
    }

    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return $"FlowTransient({typeof(TComponent).Name}))";
    }
}