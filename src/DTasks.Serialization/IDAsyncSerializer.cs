using DTasks.Infrastructure;
using System.Buffers;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Serialization;

public interface IDAsyncSerializer
{
    void Serialize<TValue>(IBufferWriter<byte> buffer, TValue value);
    
    TValue Deserialize<TValue>(ReadOnlySpan<byte> bytes);
    
    void SerializeStateMachine<TStateMachine>(IBufferWriter<byte> buffer, ISuspensionContext context, DAsyncId parentId, ref TStateMachine stateMachine)
        where TStateMachine : notnull;

    DAsyncLink DeserializeStateMachine(IResumptionContext context, ReadOnlySpan<byte> bytes);

    DAsyncLink DeserializeStateMachine<TResult>(IResumptionContext context, ReadOnlySpan<byte> bytes, TResult result);

    DAsyncLink DeserializeStateMachine(IResumptionContext context, ReadOnlySpan<byte> bytes, Exception exception);
}
