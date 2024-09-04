namespace DTask.Tests.Interceptors.Emit;

internal enum ArgumentType
{
    None,
    Byte,
    Short,
    Int,
    Long,
    Float,
    Double,
    String,
    Type,
    MethodInfo,
    ConstructorInfo,
    FieldInfo,
    LocalBuilder,
    Label,
    LabelArray,
    SignatureHelper
}

internal static class ArgumentTypeExtensions
{
    public static string ToSourceString(this ArgumentType argumentType) => argumentType switch
    {
        ArgumentType.None            => "",
        ArgumentType.Byte            => "byte",
        ArgumentType.Short           => "short",
        ArgumentType.Int             => "int",
        ArgumentType.Long            => "long",
        ArgumentType.Float           => "float",
        ArgumentType.Double          => "double",
        ArgumentType.String          => "string",
        ArgumentType.Type            => "Type",
        ArgumentType.MethodInfo      => "MethodInfo",
        ArgumentType.ConstructorInfo => "ConstructorInfo",
        ArgumentType.FieldInfo       => "FieldInfo",
        ArgumentType.LocalBuilder    => "LocalBuilder",
        ArgumentType.Label           => "Label",
        ArgumentType.LabelArray      => "Label[]",
        ArgumentType.SignatureHelper => "SignatureHelper",
        _                            => throw new ArgumentOutOfRangeException(nameof(argumentType))
    };
}