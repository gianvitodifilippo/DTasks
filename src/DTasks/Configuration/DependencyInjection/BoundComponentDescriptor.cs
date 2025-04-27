using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace DTasks.Configuration.DependencyInjection;

[DebuggerDisplay("{ToString(),nq}")]
internal sealed class BoundComponentDescriptor<TComponent, TDependency>(
    IComponentDescriptor<TDependency> dependencyDescriptor,
    DescriptorResolver<TComponent, TDependency> resolve) : IComponentDescriptor<TComponent>
    where TComponent : notnull
    where TDependency : notnull
{
    public TReturn Build<TReturn>(IDAsyncInfrastructureBuilder<TComponent, TReturn> builder)
    {
        return builder.Bind(dependencyDescriptor, resolve);
    }

    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return $"Binding({dependencyDescriptor} -> {typeof(TComponent).Name}))";
    }
}
