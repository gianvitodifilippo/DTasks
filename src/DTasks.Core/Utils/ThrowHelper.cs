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
}
