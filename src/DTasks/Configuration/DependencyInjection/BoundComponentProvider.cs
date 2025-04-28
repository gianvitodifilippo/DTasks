using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using DTasks.Infrastructure;

namespace DTasks.Configuration.DependencyInjection;

[DebuggerDisplay("{ToString(),nq}")]
internal sealed class BoundComponentProvider<TComponent, TDependency>(
    IComponentProvider<TDependency> dependencyProvider,
    IComponentDescriptor<TComponent> descriptor,
    DescriptorResolver<TComponent, TDependency> resolve) : FlowComponentProvider<TComponent>
    where TComponent : notnull
    where TDependency : notnull
{
    public override TComponent GetComponent(IDAsyncScope scope) => scope.GetBoundComponent(dependencyProvider, descriptor, resolve);

    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return $"Binding({dependencyProvider} -> {typeof(TComponent).Name}))";
    }
}