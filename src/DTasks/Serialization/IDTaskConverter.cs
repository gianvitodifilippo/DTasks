using DTasks.Hosting;

namespace DTasks.Serialization;

public interface IDTaskConverter<THeap>
{
    THeap CreateHeap(IDTaskScope scope);

    THeap DeserializeHeap(IDTaskScope scope, ReadOnlySpan<byte> bytes);

    DTask DeserializeStateMachine(ref THeap heap, ReadOnlySpan<byte> bytes, DTask resultTask);

    T Deserialize<T>(ReadOnlySpan<byte> bytes);

    ReadOnlyMemory<byte> SerializeHeap(ref THeap heap);

    ReadOnlyMemory<byte> SerializeStateMachine<TStateMachine>(ref THeap heap, ref TStateMachine stateMachine, IStateMachineInfo info)
        where TStateMachine : notnull;

    ReadOnlyMemory<byte> Serialize<T>(T value);
}
