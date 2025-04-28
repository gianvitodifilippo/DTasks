using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using DTasks.Configuration;
using DTasks.Configuration.DependencyInjection;
using DTasks.Inspection;
using DTasks.Inspection.Dynamic;
using DTasks.Serialization.Configuration;
using DTasks.Serialization.Json.Converters;
using DTasks.Utils;

namespace DTasks.Serialization.Json.Configuration;

internal sealed class JsonFormatConfigurationBuilder : IJsonFormatConfigurationBuilder
{
    private readonly List<Action<JsonSerializerOptions, DTasksConfiguration>> _optionsConfigurationActions = [];

    private IComponentDescriptor<IStateMachineInspector> _inspectorDescriptor = ComponentDescriptor.Singleton(
        static configuration => DynamicStateMachineInspector.Create(
            typeof(IStateMachineSuspender<>),
            typeof(IStateMachineResumer),
            configuration.TypeResolver));

    public TBuilder Configure<TBuilder>(TBuilder builder)
        where TBuilder : ISerializationConfigurationBuilder
    {
        var serializerFactoryDescriptor = _inspectorDescriptor.Map((configuration, inspector) =>
        {
            JsonSerializerOptions serializerOptions = new();
            foreach (var configure in _optionsConfigurationActions)
            {
                configure(serializerOptions, configuration);
            }

            serializerOptions.AllowOutOfOrderMetadataProperties = false;
            serializerOptions.ReferenceHandler = ReferenceHandler.Preserve;

            ImmutableArray<Type> surrogatableTypes = configuration.SurrogatableTypes;
            FrozenSet<Type> surrogatableNonGenericTypes = surrogatableTypes
                .Where(type => !type.IsGenericType)
                .ToFrozenSet();
            FrozenSet<Type> surrogatableGenericTypes = surrogatableTypes
                .Where(type => type.IsGenericType)
                .ToFrozenSet();

            serializerOptions.Converters.Add(new TypeIdJsonConverter());
            serializerOptions.Converters.Add(new DAsyncIdJsonConverter());

            return new JsonDAsyncSerializerFactory(
                inspector,
                configuration.TypeResolver,
                surrogatableNonGenericTypes,
                surrogatableGenericTypes,
                serializerOptions);
        });

        var serializerDescriptor = serializerFactoryDescriptor.Map(factory => factory.CreateSerializer());
        var stateMachineSerializerDescriptor = serializerFactoryDescriptor
            .Map((flow, serializerFactory) => serializerFactory.CreateStateMachineSerializer(flow.Surrogator));

        builder
            .UseSerializer(serializerDescriptor)
            .UseStateMachineSerializer(stateMachineSerializerDescriptor);

        return builder;
    }

    IJsonFormatConfigurationBuilder IJsonFormatConfigurationBuilder.ConfigureSerializerOptions(Action<JsonSerializerOptions> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        _optionsConfigurationActions.Add((options, configuration) => configure(options));
        return this;
    }

    IJsonFormatConfigurationBuilder IJsonFormatConfigurationBuilder.ConfigureSerializerOptions(Action<JsonSerializerOptions, DTasksConfiguration> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        _optionsConfigurationActions.Add(configure);
        return this;
    }

    IJsonFormatConfigurationBuilder IJsonFormatConfigurationBuilder.UseInspector(IComponentDescriptor<IStateMachineInspector> descriptor)
    {
        ThrowHelper.ThrowIfNull(descriptor);

        _inspectorDescriptor = descriptor;
        return this;
    }
}