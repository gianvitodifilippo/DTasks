using DTasks.Infrastructure.Marshaling;
using DTasks.Inspection;
using DTasks.Serialization.Json.Converters;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DTasks.Serialization.Json;

internal sealed class JsonDAsyncSerializerFactory
{
    private static readonly ImmutableArray<JsonConverter> s_wellKnownConverters =
    [
        new TypeIdJsonConverter(),
        new DAsyncIdJsonConverter()
    ];

    private readonly IStateMachineInspector _inspector;
    private readonly IDAsyncTypeResolver _typeResolver;
    private readonly FrozenSet<Type> _surrogatableNonGenericTypes;
    private readonly FrozenSet<Type> _surrogatableGenericTypes;
    private readonly JsonSerializerOptions _options;

    public JsonDAsyncSerializerFactory(
        IStateMachineInspector inspector,
        IDAsyncTypeResolver typeResolver,
        FrozenSet<Type> surrogatableNonGenericTypes,
        FrozenSet<Type> surrogatableGenericTypes,
        JsonSerializerOptions options)
    {
        _inspector = inspector;
        _typeResolver = typeResolver;
        _surrogatableNonGenericTypes = surrogatableNonGenericTypes;
        _surrogatableGenericTypes = surrogatableGenericTypes;
        _options = options;
    }

    public IDAsyncSerializer CreateSerializer()
    {
        return new JsonDAsyncSerializer(_typeResolver, _options);
    }

    public IStateMachineSerializer CreateStateMachineSerializer(IDAsyncSurrogator surrogator)
    {
        JsonSerializerOptions surrogatorOptions = new(_options);
        JsonSerializerOptions defaultOptions = new(_options);

        StateMachineReferenceResolver referenceResolver = new();
        surrogatorOptions.ReferenceHandler = new StateMachineReferenceHandler(referenceResolver);
        defaultOptions.ReferenceHandler = new StateMachineReferenceHandler(referenceResolver);

        object[] converterConstructorArguments = [surrogator, defaultOptions];

        foreach (Type surrogatableType in _surrogatableNonGenericTypes)
        {
            Type surrogatableConverterType = typeof(SurrogatableConverter<>).MakeGenericType(surrogatableType);
            JsonConverter surrogatableConverter = (JsonConverter)Activator.CreateInstance(surrogatableConverterType, converterConstructorArguments)!;
            surrogatorOptions.Converters.Add(surrogatableConverter);
        }

        foreach (JsonConverter wellKnownConverter in s_wellKnownConverters)
        {
            surrogatorOptions.Converters.Add(wellKnownConverter);
        }

        SurrogatableConverterFactory surrogatableConverterFactory = new(_surrogatableGenericTypes, converterConstructorArguments);
        surrogatorOptions.Converters.Add(surrogatableConverterFactory);

        defaultOptions.TypeInfoResolverChain.Add(new SurrogatorJsonTypeInfoResolver(defaultOptions));

        return new JsonStateMachineSerializer(_inspector, _typeResolver, referenceResolver, surrogatorOptions);
    }
}
