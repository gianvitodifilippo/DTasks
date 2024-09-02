using DTasks.Hosting;

namespace DTasks.Serialization;

public interface IDTaskConverter<THeap>
    where THeap : notnull, IFlowHeap
{
    THeap CreateHeap(IDTaskScope scope);

    THeap DeserializeHeap(IDTaskScope scope, ReadOnlyMemory<byte> bytes);

    ReadOnlyMemory<byte> SerializeHeap(ref THeap heap);

    void DisposeHeap(ref THeap heap);

    ReadOnlyMemory<byte> SerializeStateMachine<TStateMachine>(ref THeap heap, ref TStateMachine stateMachine, IStateMachineInfo info)
        where TStateMachine : notnull;

    DTask DeserializeStateMachine(ref THeap heap, ReadOnlyMemory<byte> bytes, DTask resultTask);
}

