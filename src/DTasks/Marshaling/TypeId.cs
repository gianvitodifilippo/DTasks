using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace DTasks.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct TypeId(string value) : IEquatable<TypeId>
{
    public string Value { get; } = value;

    public bool Equals(TypeId other)
    {
        return Value == other.Value;
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is TypeId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return Value;
    }

    public static bool operator ==(TypeId left, TypeId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TypeId left, TypeId right)
    {
        return !(left == right);
    }
}
