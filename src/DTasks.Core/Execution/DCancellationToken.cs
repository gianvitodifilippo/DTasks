using System.Diagnostics.CodeAnalysis;

namespace DTasks.Execution;

public readonly struct DCancellationToken : IEquatable<DCancellationToken>
{
    private readonly DCancellationTokenSource? _source;

    internal DCancellationToken(DCancellationTokenSource source)
    {
        _source = source;
    }

    public static DCancellationToken None => default;

    //public static implicit operator CancellationToken(DCancellationToken token) => token._source?.Token ?? default;

    public bool Equals(DCancellationToken other) => _source == other._source;

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is DCancellationToken other && Equals(other);

    public override int GetHashCode() => _source.GetHashCode();

    public static bool operator ==(DCancellationToken left, DCancellationToken right) => left.Equals(right);

    public static bool operator !=(DCancellationToken left, DCancellationToken right) => !(left == right);

    public static CancellationDAwaitable CreateSourceAsync(CancellationToken cancellationToken = default) => new(
        new CancellationFactoryArguments(default, cancellationToken),
        static (arguments, factory) => factory.Create(arguments.CancellationToken));

    public static CancellationDAwaitable CreateSourceAsync(TimeSpan delay, CancellationToken cancellationToken = default) => new(
        new CancellationFactoryArguments(delay, cancellationToken),
        static (arguments, factory) => factory.Create(arguments.Delay, arguments.CancellationToken));
}
