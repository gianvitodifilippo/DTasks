using System.Collections.Frozen;
using System.Text.Json;
using System.Text.Json.Serialization;
using DTasks.Infrastructure;
using DTasks.Infrastructure.Generics;
using DTasks.Infrastructure.Marshaling;
using DTasks.Inspection;
using DTasks.Serialization.Json.Converters;

namespace DTasks.Serialization.Json;

internal sealed class JsonDAsyncSerializerFactory
{
    private readonly IStateMachineInspector _inspector;
    private readonly IDAsyncTypeResolver _typeResolver;
    private readonly FrozenSet<ITypeContext> _surrogatableTypeContexts;
    private readonly JsonSerializerOptions _options;

    public JsonDAsyncSerializerFactory(
        IStateMachineInspector inspector,
        IDAsyncTypeResolver typeResolver,
        FrozenSet<ITypeContext> surrogatableTypeContexts,
        JsonSerializerOptions options)
    {
        _inspector = inspector;
        _typeResolver = typeResolver;
        _surrogatableTypeContexts = surrogatableTypeContexts;
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
        foreach (ITypeContext typeContext in _surrogatableTypeContexts)
        {
            typeContext.Execute(ref addConverterAction);
        }

        defaultOptions.TypeInfoResolverChain.Add(new SurrogatorJsonTypeInfoResolver(defaultOptions));

        return new JsonStateMachineSerializer(_inspector, _typeResolver, referenceResolver, surrogatorOptions);
    }
    
    private readonly struct AddSurrogatableConverterAction(
        IDAsyncSurrogator surrogator,
        JsonSerializerOptions defaultOptions,
        JsonSerializerOptions surrogatorOptions) : ITypeAction
    {
        public void Invoke<T>()
        {
            surrogatorOptions.Converters.Add(new SurrogatableConverter<T>(surrogator, defaultOptions));
        }
    }
}
