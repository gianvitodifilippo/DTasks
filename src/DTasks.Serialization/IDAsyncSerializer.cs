using DTasks.Infrastructure;
using System.Buffers;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;

namespace DTasks.Serialization;

public interface IDAsyncSerializer
{
    void Serialize<TValue>(IBufferWriter<byte> buffer, TValue value);
    
    TValue Deserialize<TValue>(ReadOnlySpan<byte> bytes);
}
