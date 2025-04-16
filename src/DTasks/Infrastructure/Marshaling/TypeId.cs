using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace DTasks.Infrastructure.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct TypeId : IEquatable<TypeId>
{
    private readonly string _value;

    internal TypeId(string value) => _value = value;

    public bool Equals(TypeId other) => _value == other._value;

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is TypeId other && Equals(other);

    public override int GetHashCode() => _value?.GetHashCode() ?? typeof(TypeId).GetHashCode();

    public override string ToString() => _value;

    public static bool operator ==(TypeId left, TypeId right) => left.Equals(right);

    public static bool operator !=(TypeId left, TypeId right) => !(left == right);

    public static TypeId Parse(string value) => new(value);
}
