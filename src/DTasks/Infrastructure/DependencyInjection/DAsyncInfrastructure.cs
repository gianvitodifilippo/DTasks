using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using DTasks.Configuration;
using DTasks.Configuration.DependencyInjection;
using DTasks.Infrastructure.Execution;
using DTasks.Infrastructure.Generics;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;

namespace DTasks.Infrastructure.DependencyInjection;

internal sealed class DAsyncInfrastructure : IDAsyncInfrastructure, IDAsyncRootInfrastructure, IDAsyncRootScope
{
    private readonly Func<IComponentProvider, IDAsyncHeap> _getHeap;
    private readonly Func<IComponentProvider, IDAsyncStack> _getStack;
    private readonly Func<IComponentProvider, IDAsyncSurrogator> _getSurrogator;
    private readonly Func<IComponentProvider, IDAsyncCancellationProvider> _getCancellationProvider;
    private readonly Func<IComponentProvider, IDAsyncSuspensionHandler> _getSuspensionHandler;
    private readonly RootComponentProvider _rootProvider;
    private readonly FrozenDictionary<object, object?> _properties;
    private readonly IDAsyncTypeResolver _typeResolver;
    private readonly FrozenSet<ITypeContext> _surrogatableTypeContexts;

    public DAsyncInfrastructure(DAsyncInfrastructureBuilder builder, DTasksConfigurationBuilder configurationBuilder)
    {
        _getHeap = builder.HeapAccessor;
        _getStack = builder.StackAccessor;
        _getSurrogator = builder.SurrogatorAccessor;
        _getCancellationProvider = builder.CancellationProviderAccessor;
        _getSuspensionHandler = builder.SuspensionHandlerAccessor;
        _rootProvider = new RootComponentProvider(this);
        _properties = configurationBuilder.Properties;
        _typeResolver = configurationBuilder.TypeResolver;
        _surrogatableTypeContexts = configurationBuilder.SurrogatableTypeContexts;
    }
    
    public IDAsyncRootInfrastructure RootInfrastructure => this;

    public IDAsyncRootScope RootScope => this;
    
    public RootComponentProvider RootProvider => _rootProvider;

    public IDAsyncTypeResolver TypeResolver => _typeResolver;

    IDAsyncTypeResolver IDAsyncRootInfrastructure.TypeResolver => _typeResolver;

    FrozenSet<ITypeContext> IDAsyncRootScope.SurrogatableTypeContexts => _surrogatableTypeContexts;

    public IDAsyncHeap GetHeap(IComponentProvider hostProvider) => _getHeap(hostProvider);

    public IDAsyncStack GetStack(IComponentProvider flowProvider) => _getStack(flowProvider);

    public IDAsyncSurrogator GetSurrogator(IComponentProvider flowProvider) => _getSurrogator(flowProvider);

    public IDAsyncCancellationProvider GetCancellationProvider(IComponentProvider flowProvider) => _getCancellationProvider(flowProvider);

    public IDAsyncSuspensionHandler GetSuspensionHandler(IComponentProvider flowProvider) => _getSuspensionHandler(flowProvider);
    
    bool IDAsyncRootScope.TryGetProperty<TProperty>(DAsyncPropertyKey<TProperty> key, [MaybeNullWhen(false)] out TProperty value)
    {
        return _properties.TryGetProperty(key, out value);
    }
}