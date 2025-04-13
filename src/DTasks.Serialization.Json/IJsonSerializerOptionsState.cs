using DTasks.Infrastructure.Marshaling;

namespace DTasks.Serialization.Json;

internal interface IJsonSerializerOptionsState
{
    IDAsyncSurrogator? Surrogator { get; }
}
