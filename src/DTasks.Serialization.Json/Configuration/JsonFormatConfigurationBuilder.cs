using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using DTasks.Configuration;
using DTasks.Configuration.DependencyInjection;
using DTasks.Infrastructure;
using DTasks.Inspection;
using DTasks.Inspection.Dynamic;
using DTasks.Serialization.Configuration;
using DTasks.Serialization.Json.Converters;
using DTasks.Utils;

namespace DTasks.Serialization.Json.Configuration;

internal sealed class JsonFormatConfigurationBuilder : IJsonFormatConfigurationBuilder
{
    private readonly List<Action<IDAsyncRootScope, JsonSerializerOptions>> _optionsConfigurationActions = [];

    private IComponentDescriptor<IStateMachineInspector> _inspectorDescriptor = ComponentDescriptor.Root(
        static rootScope => DynamicStateMachineInspector.Create(
            typeof(IStateMachineSuspender<>),
            typeof(IStateMachineResumer),
            rootScope.TypeResolver));

    public TBuilder Configure<TBuilder>(TBuilder builder)
        where TBuilder : ISerializationConfigurationBuilder
    {
        // IComponentDescriptor<JsonDAsyncSerializerFactory> serializerFactoryDescriptor =
        //     from rootScope in ComponentDescriptors.Root
        //     from inspector in _inspectorDescriptor
        //     select CreateSerializerFactory(rootScope, inspector);
        //
        // IComponentDescriptor<IDAsyncSerializer> serializerDescriptor = serializerFactoryDescriptor
        //     .Map(factory => factory.CreateSerializer());
        //
        // IComponentDescriptor<IStateMachineSerializer> stateMachineSerializerDescriptor =
        //     from flowScope in ComponentDescriptors.Flow
        //     from serializerFactory in serializerFactoryDescriptor
        //     select serializerFactory.CreateStateMachineSerializer(flowScope);

        IComponentDescriptor<JsonDAsyncSerializerFactory> serializerFactoryDescriptor = ComponentDescriptors.Root
            .Bind(rootScope => _inspectorDescriptor
                .Bind(inspector => ComponentDescriptor.Permanent(provider => CreateSerializerFactory(
                    provider.GetComponent(rootScope),
                    provider.GetComponent(inspector)))));
        
        IComponentDescriptor<IDAsyncSerializer> serializerDescriptor = serializerFactoryDescriptor
            .Bind(factory => ComponentDescriptor.Permanent(provider => provider.GetComponent(factory).CreateSerializer()));

        IComponentDescriptor<IStateMachineSerializer> stateMachineSerializerDescriptor = ComponentDescriptors.Flow
            .Bind(flowScope => serializerFactoryDescriptor
                .Bind(factory => ComponentDescriptor.Permanent(provider => provider.GetComponent(factory).CreateStateMachineSerializer(provider.GetComponent(flowScope)))));

        builder
            .UseSerializer(serializerDescriptor)
            .UseStateMachineSerializer(stateMachineSerializerDescriptor);

        return builder;
    }

    IJsonFormatConfigurationBuilder IJsonFormatConfigurationBuilder.ConfigureSerializerOptions(Action<JsonSerializerOptions> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        _optionsConfigurationActions.Add((_, options) => configure(options));
        return this;
    }

    IJsonFormatConfigurationBuilder IJsonFormatConfigurationBuilder.ConfigureSerializerOptions(Action<IDAsyncRootScope, JsonSerializerOptions> configure)
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

    private JsonDAsyncSerializerFactory CreateSerializerFactory(IDAsyncRootScope rootScope, IStateMachineInspector inspector)
    {
        JsonSerializerOptions serializerOptions = new();
        foreach (var configure in _optionsConfigurationActions)
        {
            configure(rootScope, serializerOptions);
        }

        serializerOptions.AllowOutOfOrderMetadataProperties = false;
        serializerOptions.ReferenceHandler = ReferenceHandler.Preserve;

        FrozenSet<Type> surrogatableTypes = rootScope.SurrogatableTypes;
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
            rootScope.TypeResolver,
            surrogatableNonGenericTypes,
            surrogatableGenericTypes,
            serializerOptions);
    }
}