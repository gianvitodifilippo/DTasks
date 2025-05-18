using DTasks.Infrastructure;

namespace DTasks.Configuration.DependencyInjection;

public static class ComponentDescriptors
{
    public static readonly IComponentDescriptor<IDAsyncRootScope> Root = ComponentDescriptor.Unit(ComponentDescriptor.Tokens.Root);
    public static readonly IComponentDescriptor<IDAsyncHostScope> Host = ComponentDescriptor.Unit(ComponentDescriptor.Tokens.Host);
    public static readonly IComponentDescriptor<IDAsyncFlowScope> Flow = ComponentDescriptor.Unit(ComponentDescriptor.Tokens.Flow);
}