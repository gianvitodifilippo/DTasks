using DTasks.Infrastructure;
using DTasks.Marshaling;
using System.Buffers;

namespace DTasks.Serialization;

public interface IDAsyncSerializer
{
    void SerializeStateMachine<TStateMachine>(IBufferWriter<byte> buffer, DAsyncId parentId, ref TStateMachine stateMachine, ISuspensionContext suspensionContext)
        where TStateMachine : notnull;

    DAsyncLink DeserializeStateMachine(ReadOnlySpan<byte> bytes);

    DAsyncLink DeserializeStateMachine<TResult>(ReadOnlySpan<byte> bytes, TResult result);

    DAsyncLink DeserializeStateMachine(ReadOnlySpan<byte> bytes, Exception exception);
}
