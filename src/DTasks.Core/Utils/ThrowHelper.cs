using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DTasks.Utils;

internal static class ThrowHelper
{
    internal static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (argument is null)
            throw new ArgumentNullException(paramName);
    }
    
    internal static void ThrowIfNullOrWhiteSpace([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(argument))
            throw new ArgumentNullException(paramName, $"'{paramName}' cannot be null or whitespace.");
    }
}
