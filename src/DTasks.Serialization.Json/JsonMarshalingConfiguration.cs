using DTasks.Infrastructure;
using DTasks.Infrastructure.Marshaling;
using DTasks.Inspection;
using DTasks.Inspection.Dynamic;
using System.Collections.Frozen;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DTasks.Serialization.Json;

public sealed class JsonMarshalingConfiguration : IMarshalingConfiguration // TODO: Fluent API
{
    private readonly HashSet<Type> _surrogatableTypes;
    private readonly HashSet<Type> _surrogatableGenericTypes;
    private readonly JsonSerializerOptions _options;
    private IStateMachineInspector? _inspector;

    private JsonMarshalingConfiguration()
    {
        _surrogatableTypes = [];
        _surrogatableGenericTypes = [];
        _options = new();
    }

    public void RegisterSurrogatableType(Type type)
    {
        // TODO: Validate type

        if (type.IsGenericTypeDefinition)
        {
            _surrogatableGenericTypes.Add(type);
        }
        else
        {
            _surrogatableTypes.Add(type);
        }
    }

    public void ConfigureSerializerOptions(Action<JsonSerializerOptions> configure)
    {
        configure(_options);
    }

    public void UseInspector(IStateMachineInspector inspector)
    {
        _inspector = inspector;
    }

    public JsonDAsyncSerializerFactory CreateSerializerFactory(IDAsyncTypeResolver typeResolver)
    {
        FrozenSet<Type> surrogatableTypes = _surrogatableTypes.ToFrozenSet();
        FrozenSet<Type> surrogatableGenericTypes = _surrogatableGenericTypes.ToFrozenSet();

        JsonSerializerOptions options = new(_options)
        {
            AllowOutOfOrderMetadataProperties = false,
            ReferenceHandler = ReferenceHandler.Preserve
        };

        IStateMachineInspector inspector = _inspector ?? DynamicStateMachineInspector.Create(typeof(IStateMachineSuspender<>), typeof(IStateMachineResumer), typeResolver);
        return new JsonDAsyncSerializerFactory(inspector, typeResolver, surrogatableTypes, surrogatableGenericTypes, options);
    }

    public static JsonMarshalingConfiguration Create()
    {
        JsonMarshalingConfiguration instance = new();
        DAsyncFlow.RegisterSurrogatableTypes(instance);

        return instance;
    }
}
