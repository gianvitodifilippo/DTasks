using DTasks.Hosting;

namespace DTasks.Serialization;

public interface IDTaskConverter<THeap>
    where THeap : IFlowHeap
{
    THeap CreateHeap();

    THeap DeserializeHeap(IResumptionScope scope, ReadOnlySpan<byte> bytes);

    ReadOnlySpan<byte> SerializeHeap(ref THeap heap);

    ReadOnlySpan<byte> SerializeStateMachine<TStateMachine>(ref THeap heap, ref TStateMachine stateMachine, IStateMachineInfo info)
        where TStateMachine : notnull;

    DTask DeserializeStateMachine(ref THeap heap, ReadOnlySpan<byte> bytes, DTask resultTask);
}

