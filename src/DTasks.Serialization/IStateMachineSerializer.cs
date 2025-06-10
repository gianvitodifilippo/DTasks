using System.Buffers;
using DTasks.Infrastructure.State;

namespace DTasks.Serialization;

public interface IStateMachineSerializer
{
    void Serialize<TStateMachine>(IDehydrationContext context, IBufferWriter<byte> buffer, ref TStateMachine stateMachine)
        where TStateMachine : notnull;

    void SerializeComplete(DAsyncId id, IBufferWriter<byte> buffer);

    void SerializeComplete<TResult>(DAsyncId id, IBufferWriter<byte> buffer, TResult result);

    void SerializeComplete(DAsyncId id, IBufferWriter<byte> buffer, Exception exception);

    DAsyncLink Deserialize(IHydrationContext context, ReadOnlySpan<byte> bytes);

    DAsyncLink Deserialize<TResult>(IHydrationContext context, ReadOnlySpan<byte> bytes, TResult result);

    DAsyncLink Deserialize(IHydrationContext context, ReadOnlySpan<byte> bytes, Exception exception);
    
    bool Link(ILinkContext context, Span<byte> bytes);
}