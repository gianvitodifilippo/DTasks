using DTasks.Inspection;
using DTasks.Inspection.Dynamic;
using System.Buffers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;

namespace DTasks.Serialization.Json;

public sealed class JsonDAsyncSerializer : IDAsyncSerializer
{
    private readonly IStateMachineInspector _inspector;
    private readonly IDAsyncTypeResolver _typeResolver;
    private readonly StateMachineReferenceResolver _referenceResolver;
    private readonly JsonSerializerOptionsState _optionsState;
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonDAsyncSerializer(IStateMachineInspector inspector, IDAsyncTypeResolver typeResolver, JsonSerializerOptions jsonOptions)
    {
        _inspector = inspector;
        _typeResolver = typeResolver;
        _referenceResolver = new();
        _optionsState = new();
        _jsonOptions = new JsonSerializerOptions(jsonOptions)
        {
            ReferenceHandler = new StateMachineReferenceHandler(_referenceResolver),
            Converters = { _optionsState }
        };
    }

    public void Serialize<TValue>(IBufferWriter<byte> buffer, TValue value)
    {
        using Utf8JsonWriter writer = new(buffer, new JsonWriterOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            IndentCharacter = _jsonOptions.IndentCharacter,
            Indented = _jsonOptions.WriteIndented,
            IndentSize = _jsonOptions.IndentSize,
            MaxDepth = _jsonOptions.MaxDepth,
            NewLine = _jsonOptions.NewLine,
            SkipValidation =
#if DEBUG
                true
#else
                false
#endif
        });
        
        JsonSerializer.Serialize(writer, value, _jsonOptions);
    }

    public TValue Deserialize<TValue>(ReadOnlySpan<byte> bytes)
    {
        Utf8JsonReader reader = new(bytes);
        return JsonSerializer.Deserialize<TValue>(ref reader, _jsonOptions)!;
    }

    public void SerializeStateMachine<TStateMachine>(IBufferWriter<byte> buffer, ISuspensionContext context, DAsyncId parentId, ref TStateMachine stateMachine)
        where TStateMachine : notnull
    {
        using var surrogatorScope = _optionsState.UseSurrogator(context.Surrogator);
        _referenceResolver.InitForWriting();

        JsonStateMachineWriter stateMachineWriter = new(buffer, _jsonOptions);
        IStateMachineSuspender<TStateMachine> suspender = (IStateMachineSuspender<TStateMachine>)_inspector.GetSuspender(typeof(TStateMachine));
        TypeId typeId = _typeResolver.GetTypeId(typeof(TStateMachine));

        stateMachineWriter.SerializeStateMachine(ref stateMachine, typeId, context, parentId, suspender);
        _referenceResolver.Clear();
    }

    public DAsyncLink DeserializeStateMachine(IResumptionContext context, ReadOnlySpan<byte> bytes)
    {
        using var surrogatorScope = _optionsState.UseSurrogator(context.Surrogator);
        _referenceResolver.InitForReading();

        JsonStateMachineReader stateMachineReader = new(bytes, _jsonOptions);
        DAsyncLink link = stateMachineReader.DeserializeStateMachine(_inspector, _typeResolver, null, static delegate (IStateMachineResumer resumer, object? result, ref JsonStateMachineReader reader)
        {
            return resumer.Resume(ref reader);
        });

        _referenceResolver.Clear();
        return link;
    }

    public DAsyncLink DeserializeStateMachine<TResult>(IResumptionContext context, ReadOnlySpan<byte> bytes, TResult result)
    {
        using var surrogatorScope = _optionsState.UseSurrogator(context.Surrogator);
        string json = Encoding.UTF8.GetString(bytes);
        _referenceResolver.InitForReading();

        JsonStateMachineReader stateMachineReader = new(bytes, _jsonOptions);
        DAsyncLink link = stateMachineReader.DeserializeStateMachine(_inspector, _typeResolver, result, static delegate (IStateMachineResumer resumer, TResult result, ref JsonStateMachineReader reader)
        {
            return resumer.Resume(ref reader, result);
        });

        _referenceResolver.Clear();
        return link;
    }

    public DAsyncLink DeserializeStateMachine(IResumptionContext context, ReadOnlySpan<byte> bytes, Exception exception)
    {
        throw new NotImplementedException();
    }

    public static JsonDAsyncSerializer Create(IDAsyncTypeResolver typeResolver, JsonSerializerOptions jsonOptions)
    {
        DynamicStateMachineInspector inspector = DynamicStateMachineInspector.Create(typeof(IStateMachineSuspender<>), typeof(IStateMachineResumer), typeResolver);

        return new JsonDAsyncSerializer(inspector, typeResolver, jsonOptions);
    }
}
