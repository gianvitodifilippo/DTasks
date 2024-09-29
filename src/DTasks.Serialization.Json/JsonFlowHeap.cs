using DTasks.Hosting;
using System.Buffers;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace DTasks.Serialization.Json;

public struct JsonFlowHeap : IFlowHeap
{
    private readonly ArrayBufferWriter<byte> _buffer;
    private int _startIndex;

    internal JsonFlowHeap(
        ArrayBufferWriter<byte> buffer,
        Utf8JsonWriter writer,
        DTaskReferenceResolver referenceResolver,
        JsonSerializerOptions options)
    {
        _buffer = buffer;
        Writer = writer;
        ReferenceResolver = referenceResolver;
        Options = options;
    }

    internal Utf8JsonWriter Writer { get; }

    internal DTaskReferenceResolver ReferenceResolver { get; }

    internal JsonSerializerOptions Options { get; }

    uint IFlowHeap.StackCount { get; set; }

    internal ReadOnlyMemory<byte> GetWrittenMemoryAndAdvance()
    {
        Writer.Flush();
        Writer.Reset();

        int startIndex = _startIndex;
        _startIndex = _buffer.WrittenCount;

        return _buffer.WrittenMemory[startIndex..];
    }

    internal static JsonFlowHeap Create(IDTaskScope scope, JsonSerializerOptions rootOptions)
    {
        ArrayBufferWriter<byte> buffer = new();
        Utf8JsonWriter writer = new(buffer, new JsonWriterOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Indented = rootOptions.WriteIndented
        });
        DTaskReferenceResolver referenceResolver = new(scope, rootOptions);
        JsonSerializerOptions options = new(rootOptions)
        {
            ReferenceHandler = referenceResolver.CreateHandler()
        };

        return new JsonFlowHeap(buffer, writer, referenceResolver, options);
    }
}
