using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using DTasks.Configuration;
using DTasks.Utils;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncFlowContext
{
    IDAsyncHostInfrastructure HostInfrastructure { get; }
    
    bool TryGetProperty<TProperty>(DAsyncPropertyKey<TProperty> key, [MaybeNullWhen(false)] out TProperty value);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public static class DAsyncFlowContextExtensions
{
    public static TProperty? GetProperty<TProperty>(this IDAsyncFlowContext context, DAsyncPropertyKey<TProperty> key)
    {
        ThrowHelper.ThrowIfNull(context);

        _ = context.TryGetProperty(key, out TProperty? value);
        return value;
    }
    
    public static TProperty GetRequiredProperty<TProperty>(this IDAsyncFlowContext context, DAsyncPropertyKey<TProperty> key)
    {
        ThrowHelper.ThrowIfNull(context);
        
        if (!context.TryGetProperty(key, out TProperty? value))
            throw new KeyNotFoundException($"Key '{key}' was not found in context.");
        
        return value;
    }
}
