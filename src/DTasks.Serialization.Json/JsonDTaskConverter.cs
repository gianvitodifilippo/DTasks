using DTasks.Hosting;
using DTasks.Inspection;
using System.Text.Json;

namespace DTasks.Serialization.Json;

public sealed class JsonDTaskConverter(
    IStateMachineInspector inspector,
    IStateMachineTypeResolver typeResolver,
    JsonSerializerOptions options) : IDTaskConverter<JsonFlowHeap>
{
    public JsonFlowHeap CreateHeap(IDTaskScope scope)
    {
        return JsonFlowHeap.Create(scope, options);
    }

    public ReadOnlyMemory<byte> SerializeHeap(ref JsonFlowHeap heap)
    {
        heap.ReferenceResolver.Serialize(heap.Writer, heap.Options);

        return heap.GetWrittenMemoryAndAdvance();
    }

    public JsonFlowHeap DeserializeHeap(IDTaskScope scope, ReadOnlySpan<byte> bytes)
    {
        JsonFlowHeap heap = JsonFlowHeap.Create(scope, options);

        Utf8JsonReader reader = new(bytes);
        heap.ReferenceResolver.Deserialize(ref reader, heap.Options);

        return heap;
    }

    public ReadOnlyMemory<byte> SerializeStateMachine<TStateMachine>(ref JsonFlowHeap heap, ref TStateMachine stateMachine, IStateMachineInfo info)
        where TStateMachine : notnull
    {
        Type stateMachineType = typeof(TStateMachine);
        StateMachineDeconstructor deconstructor = new StateMachineDeconstructor(ref heap);

        deconstructor.StartWriting();
        
        object typeId = typeResolver.GetTypeId(stateMachineType) ?? throw new InvalidOperationException($"The type id for state machine of type '{typeof(TStateMachine)}' was null.");
        deconstructor.WriteTypeId(typeId);
        
        var suspend = (DTaskSuspender<TStateMachine>)inspector.GetSuspender(stateMachineType);
        suspend(ref stateMachine, info, ref deconstructor);
        
        deconstructor.EndWriting();

        return heap.GetWrittenMemoryAndAdvance();
    }

    public DTask DeserializeStateMachine(ref JsonFlowHeap heap, ReadOnlySpan<byte> bytes, DTask resultTask)
    {
        var constructor = new StateMachineConstructor(bytes, ref heap);

        constructor.StartReading();

        object typeId = constructor.ReadTypeId();
        Type stateMachineType = typeResolver.GetType(typeId);

        var resume = (DTaskResumer)inspector.GetResumer(stateMachineType);
        DTask task = resume(resultTask, ref constructor);
        
        constructor.EndReading();
        return task;
    }

    public static StateMachineInspector CreateInspector() => StateMachineInspector.Create(typeof(DTaskSuspender<>), typeof(DTaskResumer));
}
