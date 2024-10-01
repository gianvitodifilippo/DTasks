namespace DTasks.Hosting;

public enum FlowKind : byte
{
    // Update FlowId.IsValidKind when adding new values
    Hosted,
    WhenAll,
    WhenAllResult,
    WhenAny,
    WhenAnyResult
}

internal static class FlowKindExtensions
{
    public static bool IsAggregate(this FlowKind kind) => kind is >= FlowKind.WhenAll and <= FlowKind.WhenAnyResult;
}
