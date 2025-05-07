using System.ComponentModel;

namespace DTasks.Configuration.DependencyInjection;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncInfrastructureBuilder<in TComponent, out TReturn>
{
    TReturn Unit(Func<IDAsyncRootScope, IDAsyncFlowScope, TComponent> factory);

    TReturn Map<TDependency>(ComponentDescriptor<TDependency> dependencyDescriptor, Func<TDependency, TComponent> map);
}
