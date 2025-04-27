using System.Collections.Immutable;
using DTasks.Infrastructure;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Configuration.DependencyInjection;

public static class ComponentDescriptors
{
    public static readonly IComponentDescriptor<DTasksConfiguration> Configuration = ComponentDescriptor.Singleton(config => config);
    public static readonly IComponentDescriptor<IDAsyncFlow> Flow = ComponentDescriptor.Scoped(flow => flow);
    public static readonly IComponentDescriptor<IDAsyncTypeResolver> TypeResolver = ComponentDescriptor.Singleton(config => config.TypeResolver);
    public static readonly IComponentDescriptor<ImmutableArray<Type>> SurrogatableTypes = ComponentDescriptor.Singleton(config => config.SurrogatableTypes);
}