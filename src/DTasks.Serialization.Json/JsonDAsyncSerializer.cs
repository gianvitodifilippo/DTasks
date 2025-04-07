using DTasks.Infrastructure;
using DTasks.Inspection;
using DTasks.Inspection.Dynamic;
using System.Buffers;
using System.Text;
using System.Text.Json;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Serialization.Json;

public sealed class JsonDAsyncSerializer : IDAsyncSerializer
{
    private readonly IStateMachineInspector _inspector;
    private readonly IDAsyncTypeResolver _typeResolver;
    private readonly IDAsyncMarshaler _marshaler;
    private readonly StateMachineReferenceResolver _referenceResolver;
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonDAsyncSerializer(IStateMachineInspector inspector, IDAsyncTypeResolver typeResolver, IDAsyncMarshaler marshaler, JsonSerializerOptions jsonOptions)
    {
        _inspector = inspector;
        _typeResolver = typeResolver;
        _marshaler = marshaler;
        _referenceResolver = new();
        _jsonOptions = new JsonSerializerOptions(jsonOptions)
        {
            ReferenceHandler = new StateMachineReferenceHandler(_referenceResolver)
        };
    }

    public void SerializeStateMachine<TStateMachine>(IBufferWriter<byte> buffer, DAsyncId parentId, ref TStateMachine stateMachine, ISuspensionContext suspensionContext)
        where TStateMachine : notnull
    {
        _referenceResolver.InitForWriting();

        JsonStateMachineWriter stateMachineWriter = new(buffer, _jsonOptions, _referenceResolver, _marshaler);
        IStateMachineSuspender<TStateMachine> suspender = (IStateMachineSuspender<TStateMachine>)_inspector.GetSuspender(typeof(TStateMachine));
        TypeId typeId = _typeResolver.GetTypeId(typeof(TStateMachine));

        stateMachineWriter.SerializeStateMachine(ref stateMachine, typeId, parentId, suspender, suspensionContext);
        _referenceResolver.Clear();
    }

    public DAsyncLink DeserializeStateMachine(ReadOnlySpan<byte> bytes)
    {
        _referenceResolver.InitForReading();

        JsonStateMachineReader stateMachineReader = new(bytes, _jsonOptions, _referenceResolver, _marshaler);
        DAsyncLink link = stateMachineReader.DeserializeStateMachine(_inspector, _typeResolver, null, static delegate (IStateMachineResumer resumer, object? result, ref JsonStateMachineReader reader)
        {
            return resumer.Resume(ref reader);
        });

        _referenceResolver.Clear();
        return link;
    }

    public DAsyncLink DeserializeStateMachine<TResult>(ReadOnlySpan<byte> bytes, TResult result)
    {
        string json = Encoding.UTF8.GetString(bytes);
        _referenceResolver.InitForReading();

        JsonStateMachineReader stateMachineReader = new(bytes, _jsonOptions, _referenceResolver, _marshaler);
        DAsyncLink link = stateMachineReader.DeserializeStateMachine(_inspector, _typeResolver, result, static delegate (IStateMachineResumer resumer, TResult result, ref JsonStateMachineReader reader)
        {
            return resumer.Resume(ref reader, result);
        });

        _referenceResolver.Clear();
        return link;
    }

    public DAsyncLink DeserializeStateMachine(ReadOnlySpan<byte> bytes, Exception exception)
    {
        throw new NotImplementedException();
    }

    public static JsonDAsyncSerializer Create(IDAsyncTypeResolver typeResolver, IDAsyncMarshaler marshaler, JsonSerializerOptions jsonOptions)
    {
        DynamicStateMachineInspector inspector = DynamicStateMachineInspector.Create(typeof(IStateMachineSuspender<>), typeof(IStateMachineResumer), typeResolver);

        return new JsonDAsyncSerializer(inspector, typeResolver, marshaler, jsonOptions);
    }
}
