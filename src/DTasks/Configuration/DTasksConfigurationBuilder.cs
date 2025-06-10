using System.Collections.Frozen;
using System.Diagnostics;
using DTasks.Configuration.DependencyInjection;
using DTasks.Infrastructure;
using DTasks.Infrastructure.DependencyInjection;
using DTasks.Infrastructure.Execution;
using DTasks.Infrastructure.Generics;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;
using DTasks.Utils;

namespace DTasks.Configuration;

internal sealed class DTasksConfigurationBuilder : IDTasksConfigurationBuilder,
    IMarshalingConfigurationBuilder,
    IExecutionConfigurationBuilder,
    IStateConfigurationBuilder
{
    private IComponentDescriptor<IDAsyncHeap>? _heapDescriptor;
    private IComponentDescriptor<IDAsyncStack>? _stackDescriptor;
    private readonly List<IComponentDescriptor<IDAsyncSurrogator>> _surrogatorDescriptors = [];
    private IComponentDescriptor<IDAsyncCancellationProvider>? _cancellationProviderDescriptor;
    private IComponentDescriptor<IDAsyncSuspensionHandler>? _suspensionHandlerDescriptor;
    private readonly Dictionary<object, object?> _properties = [];
    private readonly HashSet<ITypeContext> _surrogatableTypeContexts = [];
    private readonly Dictionary<Type, TypeId> _typesToIds = [];
    private readonly Dictionary<TypeId, ITypeContext> _idsToTypeContexts = [];
    
    public FrozenDictionary<object, object?> Properties => _properties.ToFrozenDictionary();

    public IDAsyncTypeResolver TypeResolver => DAsyncTypeResolver.Create(_typesToIds, _idsToTypeContexts);
    
    public FrozenSet<ITypeContext> SurrogatableTypeContexts => _surrogatableTypeContexts.ToFrozenSet();
    
    public DTasksConfiguration Build()
    {
        RegisterTypeId(TypeContext.Void, TypeId.FromConstant("void"));
        
        DAsyncInfrastructureBuilder infrastructureBuilder = new();
        infrastructureBuilder.UseHeap(_heapDescriptor);
        infrastructureBuilder.UseStack(_stackDescriptor);
        infrastructureBuilder.UseSurrogators(_surrogatorDescriptors);
        infrastructureBuilder.UseCancellationProvider(_cancellationProviderDescriptor);
        infrastructureBuilder.UseSuspensionHandler(_suspensionHandlerDescriptor);
        
        IDAsyncInfrastructure infrastructure = infrastructureBuilder.Build(this);
        DAsyncFlowPool flowPool = new(infrastructure);

        return new DTasksConfiguration(flowPool, infrastructure);
    }

    IDTasksConfigurationBuilder IDTasksConfigurationBuilder.SetProperty<TProperty>(DAsyncPropertyKey<TProperty> key, TProperty value)
    {
        _properties.SetProperty(key, value);
        return this;
    }

    IDTasksConfigurationBuilder IDTasksConfigurationBuilder.ConfigureMarshaling(Action<IMarshalingConfigurationBuilder> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        configure(this);
        return this;
    }

    IDTasksConfigurationBuilder IDTasksConfigurationBuilder.ConfigureState(Action<IStateConfigurationBuilder> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        configure(this);
        return this;
    }

    IDTasksConfigurationBuilder IDTasksConfigurationBuilder.ConfigureExecution(Action<IExecutionConfigurationBuilder> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        configure(this);
        return this;
    }

    IMarshalingConfigurationBuilder IMarshalingConfigurationBuilder.AddSurrogator(IComponentDescriptor<IDAsyncSurrogator> descriptor)
    {
        ThrowHelper.ThrowIfNull(descriptor);

        _surrogatorDescriptors.Add(descriptor);
        return this;
    }

    IMarshalingConfigurationBuilder IMarshalingConfigurationBuilder.RegisterSurrogatableType(ITypeContext typeContext)
    {
        _surrogatableTypeContexts.Add(typeContext);
        return this;
    }

    IMarshalingConfigurationBuilder IMarshalingConfigurationBuilder.RegisterTypeId(ITypeContext typeContext, TypeId typeId)
    {
        ThrowHelper.ThrowIfNull(typeContext);

        RegisterTypeId(typeContext, typeId);
        return this;
    }

    IExecutionConfigurationBuilder IExecutionConfigurationBuilder.UseCancellationProvider(IComponentDescriptor<IDAsyncCancellationProvider> descriptor)
    {
        ThrowHelper.ThrowIfNull(descriptor);

        _cancellationProviderDescriptor = descriptor;
        return this;
    }

    IExecutionConfigurationBuilder IExecutionConfigurationBuilder.UseSuspensionHandler(IComponentDescriptor<IDAsyncSuspensionHandler> descriptor)
    {
        ThrowHelper.ThrowIfNull(descriptor);

        _suspensionHandlerDescriptor = descriptor;
        return this;
    }

    IStateConfigurationBuilder IStateConfigurationBuilder.UseStack(IComponentDescriptor<IDAsyncStack> descriptor)
    {
        ThrowHelper.ThrowIfNull(descriptor);

        _stackDescriptor = descriptor;
        return this;
    }

    IStateConfigurationBuilder IStateConfigurationBuilder.UseHeap(IComponentDescriptor<IDAsyncHeap> descriptor)
    {
        ThrowHelper.ThrowIfNull(descriptor);

        _heapDescriptor = descriptor;
        return this;
    }

    private void RegisterTypeId(ITypeContext typeContext, TypeId typeId)
    {
        _typesToIds[typeContext.Type] = typeId;
        _idsToTypeContexts[typeId] = typeContext;
    }
}
