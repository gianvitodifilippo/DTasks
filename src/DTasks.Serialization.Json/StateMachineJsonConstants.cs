namespace DTasks.Serialization.Json;

internal static class StateMachineJsonConstants
{
    public static ReadOnlySpan<char> MarshaledValuePrefix => "%";
    public static ReadOnlySpan<byte> MarshaledValuePrefixUtf8 => "%"u8;
}
