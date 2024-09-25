namespace DTasks.Extensions.Microsoft.DependencyInjection;

internal readonly struct ServiceTypeId(string value) : IEquatable<ServiceTypeId>
{
    private readonly string _value = value;

    public bool Equals(ServiceTypeId other) => _value.Equals(other._value);

    public override bool Equals(object? obj) => obj is ServiceTypeId other && Equals(other);

    public override int GetHashCode() => _value.GetHashCode();

    public override string ToString() => _value;

    public static bool TryParse(string? value, out ServiceTypeId typeId)
    {
        if (value is null)
        {
            typeId = default;
            return false;
        }

        typeId = new(value);
        return true;
    }
}
