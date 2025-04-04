using System.Diagnostics.CodeAnalysis;

namespace DTasks.Execution;

public readonly struct DCancellationToken : IEquatable<DCancellationToken>
{
    private readonly DCancellationTokenSource? _source;

    internal DCancellationToken(DCancellationTokenSource source)
    {
        _source = source;
    }

    public DCancellationToken(bool canceled)
    {
        if (!canceled)
            return;
        
        _source = DCancellationTokenSource.CanceledSource;
    }

    public bool IsCancellationRequested => Source.IsCancellationRequested;

    public bool CanBeCanceled => _source is not null;

    private DCancellationTokenSource Source => _source ?? DCancellationTokenSource.NeverCanceledSource;

    public static DCancellationToken None => default;

    public static DCancellationToken Canceled => new(DCancellationTokenSource.CanceledSource);

    public static implicit operator CancellationToken(DCancellationToken token) => token.Source.LocalSource.Token;

    public bool Equals(DCancellationToken other) => _source == other._source;

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is DCancellationToken other && Equals(other);

    public override int GetHashCode() => Source.GetHashCode();

    public static bool operator ==(DCancellationToken left, DCancellationToken right) => left.Equals(right);

    public static bool operator !=(DCancellationToken left, DCancellationToken right) => !(left == right);
}
