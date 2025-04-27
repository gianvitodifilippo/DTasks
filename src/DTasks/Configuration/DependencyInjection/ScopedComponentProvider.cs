using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using DTasks.Infrastructure;

namespace DTasks.Configuration.DependencyInjection;

[DebuggerDisplay("{ToString(),nq}")]
internal sealed class ScopedComponentProvider<TComponent>(
    IComponentDescriptor<TComponent> descriptor,
    FlowImplementationFactory<TComponent> createComponent) : FlowComponentProvider<TComponent>
    where TComponent : notnull
{
    public override TComponent GetComponent(IDAsyncScope scope) => scope.GetScopedComponent(descriptor, createComponent);

    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return $"Scoped({typeof(TComponent).Name}))";
    }
}
