using System.Buffers;
using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using DTasks.Infrastructure.Generics;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;
using DTasks.Inspection;

namespace DTasks.Serialization.Json;

internal sealed class JsonStateMachineSerializer(
    IStateMachineInspector inspector,
    IDAsyncTypeResolver typeResolver,
    StateMachineReferenceResolver referenceResolver,
    JsonSerializerOptions serializerOptions) : IStateMachineSerializer
{
    public void Serialize<TStateMachine>(IDehydrationContext context, IBufferWriter<byte> buffer, ref TStateMachine stateMachine)
        where TStateMachine : notnull
    {
        Utf8JsonWriter writer = CreateWriter(buffer, JavaScriptEncoder.UnsafeRelaxedJsonEscaping);
        
        referenceResolver.InitForWriting();
        JsonStateMachineWriter stateMachineWriter = new(writer, serializerOptions);
        IStateMachineSuspender<TStateMachine> suspender = (IStateMachineSuspender<TStateMachine>)inspector.GetSuspender(typeof(TStateMachine));
        TypeId typeId = typeResolver.GetTypeId(typeof(TStateMachine));

        stateMachineWriter.SerializeStateMachine(ref stateMachine, typeId, context, suspender);
        referenceResolver.Clear();
    }

    public void SerializeComplete(DAsyncId id, IBufferWriter<byte> buffer)
    {
        Utf8JsonWriter writer = CreateWriter(buffer);
        TypeId typeId = typeResolver.GetTypeId(typeof(void));
        
        writer.WriteStartObject();
        writer.WriteTypeIdProperty(typeId);
        writer.WriteEndObject();
    }

    public void SerializeComplete<TResult>(DAsyncId id, IBufferWriter<byte> buffer, TResult result)
    {
        Utf8JsonWriter writer = CreateWriter(buffer);
        TypeId typeId = typeResolver.GetTypeId(typeof(TResult));
        
        writer.WriteStartObject();
        writer.WriteTypeIdProperty(typeId);
        writer.WritePropertyName("@dtasks.res");
        JsonSerializer.Serialize(writer, result, serializerOptions);
        writer.WriteEndObject();
    }

    public void SerializeComplete(DAsyncId id, IBufferWriter<byte> buffer, Exception exception)
    {
        throw new NotImplementedException();
    }

    public DAsyncLink Deserialize(IHydrationContext context, ReadOnlySpan<byte> bytes)
    {
        referenceResolver.InitForReading();
        JsonStateMachineReader stateMachineReader = new(bytes, serializerOptions);
        DAsyncLink link = stateMachineReader.DeserializeStateMachine(
            context.Id,
            inspector,
            typeResolver,
            arg: null,
            static (IStateMachineResumer resumer, object? _, ref JsonStateMachineReader reader) => resumer.Resume(ref reader));

        referenceResolver.Clear();
        return link;
    }

    public DAsyncLink Deserialize<TResult>(IHydrationContext context, ReadOnlySpan<byte> bytes, TResult result)
    {
        referenceResolver.InitForReading();
        JsonStateMachineReader stateMachineReader = new(bytes, serializerOptions);
        DAsyncLink link = stateMachineReader.DeserializeStateMachine(
            context.Id,
            inspector,
            typeResolver,
            result,
            static (IStateMachineResumer resumer, TResult result, ref JsonStateMachineReader reader) => resumer.Resume(ref reader, result));

        referenceResolver.Clear();
        return link;
    }

    public DAsyncLink Deserialize(IHydrationContext context, ReadOnlySpan<byte> bytes, Exception exception)
    {
        referenceResolver.InitForReading();
        JsonStateMachineReader stateMachineReader = new(bytes, serializerOptions);
        DAsyncLink link = stateMachineReader.DeserializeStateMachine(
            context.Id,
            inspector,
            typeResolver,
            exception,
            static (IStateMachineResumer resumer, Exception exception, ref JsonStateMachineReader reader) => resumer.Resume(ref reader, exception));

        referenceResolver.Clear();
        return link;
    }

    public bool Link(ILinkContext context, Span<byte> bytes)
    {
        Utf8JsonReader reader = new(bytes);
        
        reader.MoveNext();
        reader.ExpectToken(JsonTokenType.StartObject);

        reader.MoveNext();
        reader.ExpectPropertyName("@dtasks.tid");

        reader.MoveNext();
        TypeId typeId = reader.ReadTypeId();
        ITypeContext typeContext = typeResolver.GetTypeContext(typeId);
        
        if (typeContext.Type == typeof(void))
        {
            context.SetResult();
            return false;
        }

        reader.MoveNext();
        reader.ExpectToken(JsonTokenType.PropertyName);

        reader.MoveNext();
        if (reader.ValueTextEquals("@dtasks.res"))
        {
#if NET9_0_OR_GREATER
            SetResultAction setResultAction = new(context, reader, serializerOptions);
            typeContext.Execute(ref setResultAction);
#else
            throw new NotImplementedException();
#endif
            return false;
        }
        
        if (reader.ValueTextEquals("@dtasks.exc"))
        {
            throw new NotImplementedException();
            return false;
        }
        
        reader.ExpectPropertyName("@dtasks.pid");
        reader.MoveNext();
        DAsyncId parentId = reader.ReadDAsyncId();
        if (parentId != default)
            throw new InvalidOperationException("The state machine was already linked.");
            
        Span<char> parentIdChars = stackalloc char[DAsyncId.CharCount];
        bool success = context.ParentId.TryWriteChars(parentIdChars);
        Debug.Assert(success);
            
        Span<byte> parentIdSpan = bytes.Slice((int)reader.BytesConsumed, DAsyncId.CharCount);
        Encoding.UTF8.GetBytes(parentIdChars, parentIdSpan);
        return true;
    }
    
    private Utf8JsonWriter CreateWriter(IBufferWriter<byte> buffer, JavaScriptEncoder? encoder = null) => new(buffer, new JsonWriterOptions
    {
        Encoder = encoder,
        IndentCharacter = serializerOptions.IndentCharacter,
        Indented = serializerOptions.WriteIndented,
        IndentSize = serializerOptions.IndentSize,
        MaxDepth = serializerOptions.MaxDepth,
        NewLine = serializerOptions.NewLine,
        SkipValidation =
#if DEBUG
            true
#else
            false
#endif
    });
    
#if NET9_0_OR_GREATER
    private ref struct SetResultAction(
        ILinkContext context,
        Utf8JsonReader reader,
        JsonSerializerOptions serializerOptions) : ITypeAction
    {
        private Utf8JsonReader _reader = reader;

        public void Invoke<T>()
        {
            T result = JsonSerializer.Deserialize<T>(ref _reader, serializerOptions)!;
            context.SetResult(result);
        }
    }
#else
    private struct SetResultAction : ITypeAction
    {
        public void Invoke<T>()
        {
            throw new NotImplementedException();
        }
    }
#endif
}
