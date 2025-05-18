using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct DAsyncPropertyKey<TProperty>(object? value = null) : IEquatable<DAsyncPropertyKey<TProperty>>
{
    public DAsyncPropertyKey()
        : this(null)
    {
    }
    
    internal object Value { get; } = value ?? new();

    public bool Equals(DAsyncPropertyKey<TProperty> other) => Value.Equals(other.Value);
    
    public bool Equals<TOther>(DAsyncPropertyKey<TOther> other) => typeof(TProperty) == typeof(TOther) && Value.Equals(other.Value);

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is DAsyncPropertyKey<TProperty> other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();
    
    public static bool operator ==(DAsyncPropertyKey<TProperty> left, DAsyncPropertyKey<TProperty> right) => left.Equals(right);

    public static bool operator !=(DAsyncPropertyKey<TProperty> left, DAsyncPropertyKey<TProperty> right) => !left.Equals(right);
}