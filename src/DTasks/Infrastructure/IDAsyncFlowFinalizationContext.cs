using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncFlowFinalizationContext
{
    bool TryGetProperty<TProperty>(DAsyncFlowPropertyKey<TProperty> key, [MaybeNullWhen(false)] out TProperty value);

    bool RemoveProperty<TProperty>(DAsyncFlowPropertyKey<TProperty> key, [MaybeNullWhen(false)] out TProperty value);
}

public static class DAsyncFlowFinalizationContextExtensions
{
    public static bool RemoveProperty<TProperty>(this IDAsyncFlowFinalizationContext context, DAsyncFlowPropertyKey<TProperty> key)
    {
        return context.RemoveProperty(key, out _);
    }
}