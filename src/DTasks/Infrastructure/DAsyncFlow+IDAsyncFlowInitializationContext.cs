using System.Diagnostics.CodeAnalysis;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncFlowInitializationContext
{
    bool IDAsyncFlowInitializationContext.TryGetProperty<TProperty>(DAsyncFlowPropertyKey<TProperty> key, [MaybeNullWhen(false)] out TProperty value)
    {
        return TryGetProperty(key, out value);
    }

    void IDAsyncFlowInitializationContext.AddProperty<TProperty>(DAsyncFlowPropertyKey<TProperty> key, TProperty value)
    {
        _properties.Add(key.Key, value);
    }
}
