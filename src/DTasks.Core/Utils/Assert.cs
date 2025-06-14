using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

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
    public static void Default<T>(T value)
        where T : struct, IEquatable<T>
    {
        Debug.Assert(value.Equals(default));
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
    
    [Conditional("DEBUG")]
    public static void Equal<T>(
        T expected,
        T actual,
        [CallerArgumentExpression(nameof(expected))] string? expectedExpr = null,
        [CallerArgumentExpression(nameof(actual))] string? actualExpr = null)
    {
        Debug.Assert(EqualityComparer<T>.Default.Equals(expected, actual), $"Expected '{actualExpr}' to be equal to '{expectedExpr}'.");
    }
}
