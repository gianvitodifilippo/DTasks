using System.ComponentModel;

namespace DTasks.Utils;

[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct Option<TValue> : IEquatable<Option<TValue>>
{
    private readonly TValue _value;

    public Option(TValue value)
        : this(value, hasValue: true)
    {
    }

    private Option(TValue value, bool hasValue)
    {
        _value = value;
        HasValue = hasValue;
    }

    public bool HasValue { get; }

    public TValue Value
    {
        get
        {
            if (!HasValue)
                throw new InvalidOperationException("Optiona");

            return _value;
        }
    }

    public static Option<TValue> None => new(default!, hasValue: false);

    public bool Equals(Option<TValue> other) =>
        HasValue == other.HasValue &&
        ValuesEqual(_value, other._value);

    public override bool Equals(object? obj) => obj is Option<TValue> option && Equals(option);

    public override int GetHashCode() => HashCode.Combine(HasValue, _value);

    private static bool ValuesEqual(TValue left, TValue right)
    {
        if (left is null)
            return right is null;

        if (right is null)
            return false;

        return left.Equals(right);
    }

    public static bool operator ==(Option<TValue> left, Option<TValue> right) => left.Equals(right);

    public static bool operator !=(Option<TValue> left, Option<TValue> right) => !(left == right);

    public static Option<TValue> Some(TValue value) => new(value);

    public static implicit operator Option<TValue>(TValue value) => new(value);
}