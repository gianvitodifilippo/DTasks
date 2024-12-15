using DTasks.Hosting;
using DTasks.Inspection;
using DTasks.Marshaling;
using System.Buffers;
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
        JsonStateMachineWriter stateMachineWriter = new(buffer, _jsonOptions, _referenceResolver, _marshaler);
        IStateMachineSuspender<TStateMachine> suspender = (IStateMachineSuspender<TStateMachine>)_inspector.GetSuspender(typeof(TStateMachine));
        TypeId typeId = _typeResolver.GetTypeId(typeof(TStateMachine));

        stateMachineWriter.SerializeStateMachine(ref stateMachine, typeId, parentId, suspender, suspensionContext);
    }

    public DAsyncLink DeserializeStateMachine(ReadOnlySpan<byte> bytes)
    {
        JsonStateMachineReader stateMachineReader = new(bytes, _jsonOptions, _referenceResolver, _marshaler);
        return stateMachineReader.DeserializeStateMachine(_inspector, _typeResolver, null, static delegate (IStateMachineResumer resumer, object? result, ref JsonStateMachineReader reader)
        {
            return resumer.Resume(ref reader);
        });
    }

    public DAsyncLink DeserializeStateMachine<TResult>(ReadOnlySpan<byte> bytes, TResult result)
    {
        JsonStateMachineReader stateMachineReader = new(bytes, _jsonOptions, _referenceResolver, _marshaler);
        return stateMachineReader.DeserializeStateMachine(_inspector, _typeResolver, result, static delegate (IStateMachineResumer resumer, TResult result, ref JsonStateMachineReader reader)
        {
            return resumer.Resume(ref reader, result);
        });
    }

    public DAsyncLink DeserializeStateMachine(ReadOnlySpan<byte> bytes, Exception exception)
    {
        throw new NotImplementedException();
    }
}
