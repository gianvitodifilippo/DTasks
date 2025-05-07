using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using DTasks.Infrastructure;
using DTasks.Infrastructure.Execution;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;
using DTasks.Utils;

namespace DTasks.Configuration;

internal sealed class DTasksConfigurationBuilder : IDTasksConfigurationBuilder,
    IMarshalingConfigurationBuilder,
    IExecutionConfigurationBuilder,
    IStateConfigurationBuilder
{
    private readonly DAsyncInfrastructureBuilder _infrastructureBuilder = new();
    private readonly HashSet<Type> _surrogatableTypes = [];
    private readonly Dictionary<Type, TypeId> _typesToIds = [];
    private readonly Dictionary<TypeId, Type> _idsToTypes = [];

    public DTasksConfiguration Build()
    {
        DAsyncFlowPool flowPool = new();
        DAsyncRootScope rootScope = new(this);
        IDAsyncInfrastructure infrastructure = _infrastructureBuilder.Build(rootScope);

        return new DTasksConfiguration(flowPool, infrastructure);
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

    IMarshalingConfigurationBuilder IMarshalingConfigurationBuilder.AddSurrogator(RootComponentFactory<IDAsyncSurrogator> factory)
    {
        ThrowHelper.ThrowIfNull(factory);

        _infrastructureBuilder.AddSurrogator(factory);
        return this;
    }

    IMarshalingConfigurationBuilder IMarshalingConfigurationBuilder.RegisterSurrogatableType(Type type)
    {
        ThrowHelper.ThrowIfNull(type);

        _surrogatableTypes.Add(type);
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

        if (type.ContainsGenericParameters)
            throw new ArgumentException("Open generic types are not supported.", nameof(type));
        
        var typeId = TypeId.FromConstant(idValue);
        return RegisterTypeId(typeId, type.UnderlyingSystemType);
    }

    IExecutionConfigurationBuilder IExecutionConfigurationBuilder.UseCancellationProvider(RootComponentFactory<IDAsyncCancellationProvider> factory)
    {
        ThrowHelper.ThrowIfNull(factory);

        _infrastructureBuilder.UseCancellationProvider(factory);
        return this;
    }

    IExecutionConfigurationBuilder IExecutionConfigurationBuilder.UseSuspensionHandler(RootComponentFactory<IDAsyncSuspensionHandler> factory)
    {
        ThrowHelper.ThrowIfNull(factory);

        _infrastructureBuilder.UseSuspensionHandler(factory);
        return this;
    }

    IStateConfigurationBuilder IStateConfigurationBuilder.UseStack(FlowComponentFactory<IDAsyncStack> factory)
    {
        ThrowHelper.ThrowIfNull(factory);

        _infrastructureBuilder.UseStack(factory);
        return this;
    }

    IStateConfigurationBuilder IStateConfigurationBuilder.UseHeap(RootComponentFactory<IDAsyncHeap> factory)
    {
        ThrowHelper.ThrowIfNull(factory);

        _infrastructureBuilder.UseHeap(factory);
        return this;
    }

    private IMarshalingConfigurationBuilder RegisterTypeId(TypeId typeId, Type type)
    {
        _typesToIds[type] = typeId;
        _idsToTypes[typeId] = type;
        
        Debug.Assert(_typesToIds.Count == _idsToTypes.Count);

        return this;
    }

    private sealed class DAsyncRootScope(DTasksConfigurationBuilder builder) : IDAsyncRootScope
    {
        public IDAsyncTypeResolver TypeResolver { get; } = new DAsyncTypeResolver(
            builder._typesToIds.ToFrozenDictionary(),
            builder._idsToTypes.ToFrozenDictionary());

        public ImmutableArray<Type> SurrogatableTypes { get; } = [.. builder._surrogatableTypes];
    }
}
