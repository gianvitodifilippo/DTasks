using DTasks.Hosting;

namespace DTasks.Serialization;

public interface IDTaskConverter<THeap>
{
    THeap CreateHeap(IDTaskScope scope);

    THeap DeserializeHeap<TFlowId>(TFlowId flowId, IDTaskScope scope, ReadOnlySpan<byte> bytes)
        where TFlowId : notnull;

    DTask DeserializeStateMachine<TFlowId>(TFlowId flowId, ref THeap heap, ReadOnlySpan<byte> bytes, DTask resultTask)
        where TFlowId : notnull;

    ReadOnlyMemory<byte> SerializeHeap(ref THeap heap);

    ReadOnlyMemory<byte> SerializeStateMachine<TStateMachine>(ref THeap heap, ref TStateMachine stateMachine, IStateMachineInfo info)
        where TStateMachine : notnull;
}
