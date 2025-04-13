namespace DTasks.Serialization.Json;

internal static class StateMachineJsonConstants
{
    public static ReadOnlySpan<char> SurrogatedValuePrefix => "%";
    public static ReadOnlySpan<byte> SurrogatedValuePrefixUtf8 => "%"u8;
}
