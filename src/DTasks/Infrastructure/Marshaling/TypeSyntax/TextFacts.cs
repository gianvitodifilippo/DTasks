using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DTasks.Infrastructure.Marshaling.TypeSyntax;

internal static class TextFacts
{
    private static Regex _assemblyCultureRegex = new Regex(
        @"^(?<language>[a-zA-Z]{2,3})" +
        @"(?:-(?<script>[a-zA-Z]{4}))?" +
        @"(?:-(?<region>[a-zA-Z]{2}|\d{3}))?" +
        @"(?:-(?<variant>(\d[a-zA-Z0-9]{3}|[a-zA-Z0-9]{5,8})))?$",
        RegexOptions.None,
        TimeSpan.FromMilliseconds(500));

    public static bool IsValidCulture(ReadOnlySpan<char> value, [NotNullWhen(true)] out string? culture)
    {
        const string neutralCulture = "neutral";
        if (value.SequenceEqual(neutralCulture))
        {
            culture = neutralCulture;
            return true;
        }
        
#if NET8_0_OR_GREATER
        if (_assemblyCultureRegex.IsMatch(value))
        {
            culture = value.ToString();
            return true;
        }
        
        culture = null;
        return false;
#else
        culture = value.ToString();
        return _assemblyCultureRegex.IsMatch(culture);
#endif
    }
}