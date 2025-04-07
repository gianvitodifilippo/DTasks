using System.Diagnostics.CodeAnalysis;

namespace DTasks.AspNetCore;

public readonly struct DAsyncOperationId : IEquatable<DAsyncOperationId>
{
    private readonly Guid _value; // TODO: Switch to something else in the future

    private DAsyncOperationId(Guid value)
    {
        _value = value;
    }

    public bool Equals(DAsyncOperationId other) => _value.Equals(other._value);

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is DAsyncOperationId other && Equals(other);

    public override int GetHashCode() => _value.GetHashCode();

    public override string ToString() => _value.ToString();
    
    public static DAsyncOperationId New() => new(Guid.NewGuid());
}