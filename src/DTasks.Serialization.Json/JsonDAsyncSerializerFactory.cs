using System.Collections.Frozen;
using System.Text.Json;
using System.Text.Json.Serialization;
using DTasks.Infrastructure;
using DTasks.Infrastructure.Marshaling;
using DTasks.Inspection;
using DTasks.Serialization.Json.Converters;

namespace DTasks.Serialization.Json;

internal sealed class JsonDAsyncSerializerFactory
{
    private readonly IStateMachineInspector _inspector;
    private readonly IDAsyncTypeResolver _typeResolver;
    private readonly FrozenSet<ISurrogatableTypeContext> _surrogatableTypes;
    private readonly JsonSerializerOptions _options;

    public JsonDAsyncSerializerFactory(
        IStateMachineInspector inspector,
        IDAsyncTypeResolver typeResolver,
        FrozenSet<ISurrogatableTypeContext> surrogatableTypes,
        JsonSerializerOptions options)
    {
        _inspector = inspector;
        _typeResolver = typeResolver;
        _surrogatableTypes = surrogatableTypes;
        _options = options;
    }

    public IDAsyncSerializer CreateSerializer()
    {
        return new JsonDAsyncSerializer(_typeResolver, _options);
    }

    public IStateMachineSerializer CreateStateMachineSerializer(IDAsyncFlowScope flowScope)
    {
        JsonSerializerOptions surrogatorOptions = new(_options);
        JsonSerializerOptions defaultOptions = new(_options);

        StateMachineReferenceResolver referenceResolver = new();
        surrogatorOptions.ReferenceHandler = new StateMachineReferenceHandler(referenceResolver);
        defaultOptions.ReferenceHandler = new StateMachineReferenceHandler(referenceResolver);

        AddSurrogatableConverterAction addConverterAction = new(flowScope.Surrogator, defaultOptions, surrogatorOptions);
        foreach (ISurrogatableTypeContext typeContext in _surrogatableTypes)
        {
            typeContext.Execute(ref addConverterAction);
        }

        defaultOptions.TypeInfoResolverChain.Add(new SurrogatorJsonTypeInfoResolver(defaultOptions));

        return new JsonStateMachineSerializer(_inspector, _typeResolver, referenceResolver, surrogatorOptions);
    }
    
    private readonly struct AddSurrogatableConverterAction(
        IDAsyncSurrogator surrogator,
        JsonSerializerOptions defaultOptions,
        JsonSerializerOptions surrogatorOptions) : ISurrogatableTypeAction
    {
        public void Invoke<TSurrogatable>()
        {
            surrogatorOptions.Converters.Add(new SurrogatableConverter<TSurrogatable>(surrogator, defaultOptions));
        }
    }
}
