using System.Buffers;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;

namespace DTasks.Serialization;

public interface IStateMachineSerializer
{
    void SerializeStateMachine<TStateMachine>(IBufferWriter<byte> buffer, ISuspensionContext context, DAsyncId parentId, ref TStateMachine stateMachine)
        where TStateMachine : notnull;

    DAsyncLink DeserializeStateMachine(IResumptionContext context, ReadOnlySpan<byte> bytes);

    DAsyncLink DeserializeStateMachine<TResult>(IResumptionContext context, ReadOnlySpan<byte> bytes, TResult result);

    DAsyncLink DeserializeStateMachine(IResumptionContext context, ReadOnlySpan<byte> bytes, Exception exception);
}