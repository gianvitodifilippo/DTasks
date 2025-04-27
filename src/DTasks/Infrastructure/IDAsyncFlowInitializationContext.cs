using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncFlowInitializationContext
{
    bool TryGetProperty<TProperty>(DAsyncFlowPropertyKey<TProperty> key, [MaybeNullWhen(false)] out TProperty value);

    void AddProperty<TProperty>(DAsyncFlowPropertyKey<TProperty> key, TProperty value);
}