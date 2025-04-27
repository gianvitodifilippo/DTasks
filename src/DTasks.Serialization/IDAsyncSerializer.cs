using System.Buffers;

namespace DTasks.Serialization;

public interface IDAsyncSerializer
{
    void Serialize<TValue>(IBufferWriter<byte> buffer, TValue value);

    TValue Deserialize<TValue>(ReadOnlySpan<byte> bytes);
}
