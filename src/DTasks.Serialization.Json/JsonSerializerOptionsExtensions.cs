using System.Text.Json;

namespace DTasks.Serialization.Json;

internal static class JsonSerializerOptionsExtensions
{
    public static IJsonSerializerOptionsState GetState(this JsonSerializerOptions options)
    {
        return (IJsonSerializerOptionsState)options.GetConverter(typeof(JsonSerializerOptionsState));
    }
}
