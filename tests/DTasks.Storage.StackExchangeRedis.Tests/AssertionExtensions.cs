using System.Runtime.CompilerServices;
using Xunit.Sdk;

namespace DTasks.Storage.StackExchangeRedis;

public static class AssertionExtensions
{
    public static ReadOnlySpanAssertions<T> Should<T>(this ReadOnlySpan<T> span, [CallerArgumentExpression(nameof(span))] string subjectExpression = "span")
    {
        return new ReadOnlySpanAssertions<T>(span, subjectExpression);
    }
}

public readonly ref struct ReadOnlySpanAssertions<T>
{
    private readonly ReadOnlySpan<T> _span;
    private readonly string _subjectExpression;

    public ReadOnlySpanAssertions(ReadOnlySpan<T> span, string subjectExpression)
    {
        _span = span;
        _subjectExpression = subjectExpression;
    }

    public void BeEquivalentTo(T[] array)
    {
        ReadOnlySpan<T> other = array.AsSpan();
        if (!_span.SequenceEqual(other))
            throw FailException.ForFailure($"Expected {_subjectExpression} to be {ToDisplayString(other)}, but found {ToDisplayString(_span)}.");
    }

    private static string ToDisplayString(ReadOnlySpan<T> span)
    {
        return $"[{string.Join(", ", span.ToArray())}]";
    }
}
