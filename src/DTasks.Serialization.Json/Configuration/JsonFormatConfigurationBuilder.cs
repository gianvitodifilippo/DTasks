using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using DTasks.Configuration.DependencyInjection;
using DTasks.Inspection;
using DTasks.Inspection.Dynamic;
using DTasks.Serialization.Configuration;

namespace DTasks.Serialization.Json.Configuration;

internal sealed class JsonFormatConfigurationBuilder : IJsonFormatConfigurationBuilder
{
    private readonly JsonSerializerOptions _serializerOptions = new();

    private IComponentDescriptor<IStateMachineInspector> _inspectorDescriptor = ComponentDescriptor.Singleton(
        static configuration => DynamicStateMachineInspector.Create(
            typeof(IStateMachineSuspender<>),
            typeof(IStateMachineResumer),
            configuration.TypeResolver));

    public ISerializationConfigurationBuilder Configure(ISerializationConfigurationBuilder builder)
    {
        var serializerFactoryDescriptor = _inspectorDescriptor.Map((config, inspector) =>
        {
            JsonSerializerOptions clonedOptions = new(_serializerOptions)
            {
                AllowOutOfOrderMetadataProperties = false,
                ReferenceHandler = ReferenceHandler.Preserve
            };

            ImmutableArray<Type> surrogatableTypes = config.SurrogatableTypes;
            FrozenSet<Type> surrogatableNonGenericTypes = surrogatableTypes
                .Where(type => !type.IsGenericType)
                .ToFrozenSet();
            FrozenSet<Type> surrogatableGenericTypes = surrogatableTypes
                .Where(type => type.IsGenericType)
                .ToFrozenSet();

            return new JsonDAsyncSerializerFactory(
                inspector,
                config.TypeResolver,
                surrogatableNonGenericTypes,
                surrogatableGenericTypes,
                clonedOptions);
        });

        var serializerDescriptor = serializerFactoryDescriptor.Map(factory => factory.CreateSerializer());
        var stateMachineSerializerDescriptor = serializerFactoryDescriptor
            .Map((flow, serializerFactory) => serializerFactory.CreateStateMachineSerializer(flow.Surrogator));

        return builder
            .UseSerializer(serializerDescriptor)
            .UseStateMachineSerializer(stateMachineSerializerDescriptor);
    }

    IJsonFormatConfigurationBuilder IJsonFormatConfigurationBuilder.ConfigureSerializerOptions(Action<JsonSerializerOptions> configure)
    {
        configure(_serializerOptions);
        return this;
    }

    IJsonFormatConfigurationBuilder IJsonFormatConfigurationBuilder.UseInspector(IComponentDescriptor<IStateMachineInspector> descriptor)
    {
        _inspectorDescriptor = descriptor;
        return this;
    }
}