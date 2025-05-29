using System.Buffers;
using System.Text.Json;
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
    public void SerializeStateMachine<TStateMachine>(IBufferWriter<byte> buffer, ISuspensionContext context, ref TStateMachine stateMachine)
        where TStateMachine : notnull
    {
        referenceResolver.InitForWriting();
        JsonStateMachineWriter stateMachineWriter = new(buffer, serializerOptions);
        IStateMachineSuspender<TStateMachine> suspender = (IStateMachineSuspender<TStateMachine>)inspector.GetSuspender(typeof(TStateMachine));
        TypeId typeId = typeResolver.GetTypeId(typeof(TStateMachine));

        stateMachineWriter.SerializeStateMachine(ref stateMachine, typeId, context, suspender);
        referenceResolver.Clear();
    }

    public DAsyncLink DeserializeStateMachine(IResumptionContext context, ReadOnlySpan<byte> bytes)
    {
        referenceResolver.InitForReading();
        JsonStateMachineReader stateMachineReader = new(bytes, serializerOptions);
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
        JsonStateMachineReader stateMachineReader = new(bytes, serializerOptions);
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
