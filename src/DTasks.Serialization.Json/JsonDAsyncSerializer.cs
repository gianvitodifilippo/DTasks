using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;
using DTasks.Inspection;
using System.Buffers;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace DTasks.Serialization.Json;

internal sealed class JsonDAsyncSerializer(
    IStateMachineInspector inspector,
    IDAsyncTypeResolver typeResolver,
    StateMachineReferenceResolver referenceResolver,
    JsonSerializerOptions jsonOptions) : IDAsyncSerializer
{
    public void Serialize<TValue>(IBufferWriter<byte> buffer, TValue value)
    {
        using Utf8JsonWriter writer = new(buffer, new JsonWriterOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            IndentCharacter = jsonOptions.IndentCharacter,
            Indented = jsonOptions.WriteIndented,
            IndentSize = jsonOptions.IndentSize,
            MaxDepth = jsonOptions.MaxDepth,
            NewLine = jsonOptions.NewLine,
            SkipValidation =
#if DEBUG
                true
#else
                false
#endif
        });

        referenceResolver.InitForWriting();
        JsonSerializer.Serialize(writer, value, jsonOptions);
        referenceResolver.Clear();
    }

    public TValue Deserialize<TValue>(ReadOnlySpan<byte> bytes)
    {
        referenceResolver.InitForReading();
        Utf8JsonReader reader = new(bytes);
        TValue value = JsonSerializer.Deserialize<TValue>(ref reader, jsonOptions)!;

        referenceResolver.Clear();
        return value;
    }

    public void SerializeStateMachine<TStateMachine>(IBufferWriter<byte> buffer, ISuspensionContext context, DAsyncId parentId, ref TStateMachine stateMachine)
        where TStateMachine : notnull
    {
        referenceResolver.InitForWriting();
        JsonStateMachineWriter stateMachineWriter = new(buffer, jsonOptions);
        IStateMachineSuspender<TStateMachine> suspender = (IStateMachineSuspender<TStateMachine>)inspector.GetSuspender(typeof(TStateMachine));
        TypeId typeId = typeResolver.GetTypeId(typeof(TStateMachine));

        stateMachineWriter.SerializeStateMachine(ref stateMachine, typeId, context, parentId, suspender);
        referenceResolver.Clear();
    }

    public DAsyncLink DeserializeStateMachine(IResumptionContext context, ReadOnlySpan<byte> bytes)
    {
        referenceResolver.InitForReading();
        JsonStateMachineReader stateMachineReader = new(bytes, jsonOptions);
        DAsyncLink link = stateMachineReader.DeserializeStateMachine(inspector, typeResolver, null, static delegate (IStateMachineResumer resumer, object? result, ref JsonStateMachineReader reader)
        {
            return resumer.Resume(ref reader);
        });

        referenceResolver.Clear();
        return link;
    }

    public DAsyncLink DeserializeStateMachine<TResult>(IResumptionContext context, ReadOnlySpan<byte> bytes, TResult result)
    {
        referenceResolver.InitForReading();
        JsonStateMachineReader stateMachineReader = new(bytes, jsonOptions);
        DAsyncLink link = stateMachineReader.DeserializeStateMachine(inspector, typeResolver, result, static delegate (IStateMachineResumer resumer, TResult result, ref JsonStateMachineReader reader)
        {
            return resumer.Resume(ref reader, result);
        });

        referenceResolver.Clear();
        return link;
    }

    public DAsyncLink DeserializeStateMachine(IResumptionContext context, ReadOnlySpan<byte> bytes, Exception exception)
    {
        throw new NotImplementedException();
    }
}
