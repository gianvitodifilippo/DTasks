using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using DTasks.Configuration.DependencyInjection;
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

    public IDAsyncInfrastructure BuildInfrastructure(DTasksConfiguration configuration)
    {
        return _infrastructureBuilder.BuildInfrastructure(configuration);
    }
    
    public IDAsyncTypeResolver TypeResolver => new DAsyncTypeResolver(
        _typesToIds.ToFrozenDictionary(),
        _idsToTypes.ToFrozenDictionary());

    public ImmutableArray<Type> SurrogatableTypes => [.._surrogatableTypes];

    IDTasksConfigurationBuilder IDTasksConfigurationBuilder<IDTasksConfigurationBuilder>.ConfigureMarshaling(Action<IMarshalingConfigurationBuilder> configure)
    {
        configure(this);
        return this;
    }

    IDTasksConfigurationBuilder IDTasksConfigurationBuilder<IDTasksConfigurationBuilder>.ConfigureState(Action<IStateConfigurationBuilder> configure)
    {
        configure(this);
        return this;
    }

    IDTasksConfigurationBuilder IDTasksConfigurationBuilder<IDTasksConfigurationBuilder>.ConfigureExecution(Action<IExecutionConfigurationBuilder> configure)
    {
        configure(this);
        return this;
    }

    IMarshalingConfigurationBuilder IMarshalingConfigurationBuilder.AddSurrogator(IComponentDescriptor<IDAsyncSurrogator> descriptor)
    {
        _infrastructureBuilder.AddSurrogator(descriptor);
        return this;
    }

    IMarshalingConfigurationBuilder IMarshalingConfigurationBuilder.RegisterSurrogatableType(Type type)
    {
        _surrogatableTypes.Add(type);
        return this;
    }

    IMarshalingConfigurationBuilder IMarshalingConfigurationBuilder.RegisterTypeId(Type type)
    {
        ThrowHelper.ThrowIfNull(type);

        if (type.ContainsGenericParameters)
            throw new ArgumentException("Open generic types are not supported.", nameof(type));

        if (_typesToIds.TryGetValue(type, out TypeId id))
            return this;

        int count = _typesToIds.Count + 1;
        id = new(count.ToString()); // Naive

        _typesToIds.Add(type, id);
        _idsToTypes.Add(id, type);

        Debug.Assert(_typesToIds.Count == count && _idsToTypes.Count == count);

        return this;
    }

    IExecutionConfigurationBuilder IExecutionConfigurationBuilder.UseCancellationProvider(IComponentDescriptor<IDAsyncCancellationProvider> descriptor)
    {
        _infrastructureBuilder.UseCancellationProvider(descriptor);
        return this;
    }

    IExecutionConfigurationBuilder IExecutionConfigurationBuilder.UseSuspensionHandler(IComponentDescriptor<IDAsyncSuspensionHandler> descriptor)
    {
        _infrastructureBuilder.UseSuspensionHandler(descriptor);
        return this;
    }

    IStateConfigurationBuilder IStateConfigurationBuilder.UseStack(IComponentDescriptor<IDAsyncStack> descriptor)
    {
        _infrastructureBuilder.UseStack(descriptor);
        return this;
    }

    IStateConfigurationBuilder IStateConfigurationBuilder.UseHeap(IComponentDescriptor<IDAsyncHeap> descriptor)
    {
        _infrastructureBuilder.UseHeap(descriptor);
        return this;
    }
}