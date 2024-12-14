using DTasks.Hosting;
using DTasks.Inspection;
using DTasks.Marshaling;
using System.Buffers;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DTasks.Serialization.Json;

internal class JsonDAsyncSerializer : IDAsyncSerializer
{
    private readonly IStateMachineInspector _inspector;
    private readonly ITypeResolver _typeResolver;
    private readonly IDAsyncMarshaler _marshaler;
    private readonly ReferenceResolver _referenceResolver;
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonDAsyncSerializer(IStateMachineInspector inspector, ITypeResolver typeResolver, IDAsyncMarshaler marshaler, JsonSerializerOptions jsonOptions)
    {
        _inspector = inspector;
        _typeResolver = typeResolver;
        _marshaler = marshaler;
        _referenceResolver = ReferenceHandler.Preserve.CreateResolver();
        _jsonOptions = new JsonSerializerOptions(jsonOptions)
        {
            ReferenceHandler = new StateMachineReferenceHandler(_referenceResolver)
        };
    }

    public void SerializeStateMachine<TStateMachine>(IBufferWriter<byte> buffer, DAsyncId parentId, ref TStateMachine stateMachine, ISuspensionContext suspensionContext)
        where TStateMachine : notnull
    {
        bool skipValidation =
#if DEBUG
            false
#else
            true;
#endif

        Utf8JsonWriter jsonWriter = new(buffer, new JsonWriterOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            IndentCharacter = _jsonOptions.IndentCharacter,
            Indented = _jsonOptions.WriteIndented,
            IndentSize = _jsonOptions.IndentSize,
            MaxDepth = _jsonOptions.MaxDepth,
            NewLine = _jsonOptions.NewLine,
            SkipValidation = skipValidation
        });

        TypeId stateMachineTypeId = _typeResolver.GetTypeId(typeof(TStateMachine));

        jsonWriter.WriteStartObject();
        jsonWriter.WriteString("$typeId", stateMachineTypeId.Value);

        JsonStateMachineWriter stateMachineWriter = new(jsonWriter, _jsonOptions, _referenceResolver, _marshaler);
        IStateMachineConverter<TStateMachine> converter = (IStateMachineConverter<TStateMachine>)_inspector.GetConverter(typeof(TStateMachine));
        converter.Suspend(ref stateMachine, suspensionContext, ref stateMachineWriter);

        jsonWriter.WriteEndObject();
    }

    public DAsyncLink DeserializeStateMachine(ReadOnlySpan<byte> bytes)
    {
        throw new NotImplementedException();
    }

    public DAsyncLink DeserializeStateMachine<TResult>(ReadOnlySpan<byte> bytes, TResult result)
    {
        throw new NotImplementedException();
    }

    public DAsyncLink DeserializeStateMachine(ReadOnlySpan<byte> bytes, Exception exception)
    {
        throw new NotImplementedException();
    }
}
