using System.Diagnostics.CodeAnalysis;

namespace DTasks.AspNetCore.Http;

public readonly struct CallbackType : IEquatable<CallbackType>
{
    public static readonly CallbackType Webhook = nameof(Webhook);
    public static readonly CallbackType WebSockets = nameof(WebSockets);
    
    private readonly string _value;

    private CallbackType(string value)
    {
        _value = value;
    }

    public bool Equals(CallbackType other) => _value == other._value;

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is CallbackType other && Equals(other);

    public override int GetHashCode() => _value.GetHashCode();

    public override string ToString() => _value;

    public static implicit operator CallbackType(string value) => new(value);
    
    public static bool operator ==(CallbackType left, CallbackType right) => left.Equals(right);
    
    public static bool operator !=(CallbackType left, CallbackType right) => !(left == right);
}