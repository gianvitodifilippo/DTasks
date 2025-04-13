using DTasks.Infrastructure.Marshaling;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DTasks.Serialization.Json;

internal sealed class JsonSerializerOptionsState : JsonConverter<JsonSerializerOptionsState>, IJsonSerializerOptionsState
{
    public IDAsyncSurrogator? Surrogator { get; private set; }

    public override JsonSerializerOptionsState? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }

    public override void Write(Utf8JsonWriter writer, JsonSerializerOptionsState value, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }

    public SurrogatorScope UseSurrogator(IDAsyncSurrogator surrogator)
    {
        Debug.Assert(Surrogator is null);

        Surrogator = surrogator;
        return new SurrogatorScope(this);
    }

    public readonly ref struct SurrogatorScope(JsonSerializerOptionsState state)
    {
        public void Dispose()
        {
            Debug.Assert(state.Surrogator is not null);
            state.Surrogator = null;
        }
    }
}
