using System.Text.Json.Serialization;

namespace DTasks.AspNetCore.Execution;

internal sealed class ResumptionResult<TValue>
{
    [JsonPropertyName("value")]
    public TValue? Value { get; set; }
}
