using System.Diagnostics.CodeAnalysis;

namespace DTasks.Hosting;

public interface IDTaskScope
{
    bool TryGetReference(object token, [NotNullWhen(true)] out object? reference);

    bool TryGetReferenceToken(object reference, [NotNullWhen(true)] out object? token);
}
