using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace DTasks.Utils;

internal static class Assert
{
    [Conditional("DEBUG")]
    public static void NotNull([NotNull] object? value)
    {
        Debug.Assert(value is not null);
    }

    [Conditional("DEBUG")]
    public static void NotNull([NotNull] object? value, string message)
    {
        Debug.Assert(value is not null, message);
    }

    [Conditional("DEBUG")]
    public static void Null(object? value)
    {
        Debug.Assert(value is null);
    }

    [Conditional("DEBUG")]
    public static void Null(object? value, string message)
    {
        Debug.Assert(value is null, message);
    }

    [Conditional("DEBUG")]
    public static void Is<T>([NotNull] object? value)
    {
        Debug.Assert(value is T);
    }

    [Conditional("DEBUG")]
    public static void Is<T>([NotNull] object? value, string message)
    {
        Debug.Assert(value is T, message);
    }
}
