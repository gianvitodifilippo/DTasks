using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using DTasks.Infrastructure.Marshaling;
using DTasks.Utils;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncFlowScope
{
    IDAsyncHostScope Parent { get; }
    
    IDAsyncSurrogator Surrogator { get; }
    
    bool TryGetProperty<TProperty>(DAsyncPropertyKey<TProperty> key, [MaybeNullWhen(false)] out TProperty value);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public static class DAsyncFlowScopeExtensions
{
    public static TProperty? GetProperty<TProperty>(this IDAsyncFlowScope scope, DAsyncPropertyKey<TProperty> key)
    {
        ThrowHelper.ThrowIfNull(scope);

        _ = scope.TryGetProperty(key, out TProperty? value);
        return value;
    }
    
    public static TProperty GetRequiredProperty<TProperty>(this IDAsyncFlowScope scope, DAsyncPropertyKey<TProperty> key)
    {
        ThrowHelper.ThrowIfNull(scope);
        
        if (!scope.TryGetProperty(key, out TProperty? value))
            throw new KeyNotFoundException($"Key '{key}' was not found in scope.");
        
        return value;
    }
}

