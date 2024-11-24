using DTasks.Hosting;
using DTasks.Marshaling;
using System.Buffers;

namespace DTasks.Serialization.Json;

internal class JsonDAsyncSerializer : IDAsyncSerializer
{
    public void SerializeStateMachine<TStateMachine>(IBufferWriter<byte> buffer, DAsyncId parentId, ref TStateMachine stateMachine, ISuspensionContext suspensionContext)
        where TStateMachine : notnull
    {
        throw new NotImplementedException();
    }

    public DAsyncLink DeserializeStateMachine(ReadOnlySpan<byte> bytes)
    {
        throw new NotImplementedException();
    }

    public DAsyncLink DeserializeStateMachine<TResult>(ReadOnlySpan<byte> bytes, TResult result)
    {
        throw new NotImplementedException();
    }

    public DAsyncLink DeserializeStateMachine(ReadOnlySpan<byte> bytes, Exception exception)
    {
        throw new NotImplementedException();
    }
}
