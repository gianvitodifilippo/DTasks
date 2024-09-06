namespace DTasks.Serialization.Json;

internal static class Constants
{
    public const string TypeMetadataKey = "$type";
    public static ReadOnlySpan<byte> TypeMetadataKeyUtf8 => "$type"u8;

    public const string RefMetadataKey = "$ref";
    public static ReadOnlySpan<byte> RefMetadataKeyUtf8 => "$ref"u8;

    public const string AwaiterValue = "AWAITER";
    public static ReadOnlySpan<byte> AwaiterValueUtf8 => "AWAITER"u8;
}
