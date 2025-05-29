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
    private readonly Dictionary<TypeId, Type> _idsToTypes = [];
    
    public FrozenDictionary<object, object?> Properties => _properties.ToFrozenDictionary();

    public IDAsyncTypeResolver TypeResolver => new DAsyncTypeResolver(
        _typesToIds.ToFrozenDictionary(),
        _idsToTypes.ToFrozenDictionary());
    
    public FrozenSet<ITypeContext> SurrogatableTypeContexts => _surrogatableTypeContexts.ToFrozenSet();
    
    public DTasksConfiguration Build()
    {
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

    IMarshalingConfigurationBuilder IMarshalingConfigurationBuilder.RegisterTypeId(Type type, TypeEncodingStrategy encodingStrategy)
    {
        ThrowHelper.ThrowIfNull(type);

        if (type.ContainsGenericParameters)
            throw new ArgumentException("Open generic types are not supported.", nameof(type));

        TypeId typeId = TypeId.FromEncodedTypeName(type, encodingStrategy);
        return RegisterTypeId(typeId, type.UnderlyingSystemType);
    }

    IMarshalingConfigurationBuilder IMarshalingConfigurationBuilder.RegisterTypeId(Type type, string idValue)
    {
        ThrowHelper.ThrowIfNull(type);
        ThrowHelper.ThrowIfNullOrWhiteSpace(idValue);

        if (type.ContainsGenericParameters)
            throw new ArgumentException("Open generic types are not supported.", nameof(type));
        
        var typeId = TypeId.FromConstant(idValue);
        return RegisterTypeId(typeId, type.UnderlyingSystemType);
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

    private IMarshalingConfigurationBuilder RegisterTypeId(TypeId typeId, Type type)
    {
        _typesToIds[type] = typeId;
        _idsToTypes[typeId] = type;
        
        Debug.Assert(_typesToIds.Count == _idsToTypes.Count);

        return this;
    }
}
