using System.Diagnostics.CodeAnalysis;

namespace DTasks.Infrastructure;

internal interface IDAsyncScope : IDAsyncFlow
{
    bool TryGetComponent<TComponent>(object key, [NotNullWhen(true)] out TComponent? component)
        where TComponent : notnull;

    void AddComponent<TComponent>(object key, TComponent component)
        where TComponent : notnull;
}