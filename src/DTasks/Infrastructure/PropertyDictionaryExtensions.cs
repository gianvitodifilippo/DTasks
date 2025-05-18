using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class PropertyDictionaryExtensions
{
    public static bool TryGetProperty<TDictionary, TProperty>(this TDictionary dictionary, DAsyncPropertyKey<TProperty> key, [MaybeNullWhen(false)] out TProperty value)
        where TDictionary : IReadOnlyDictionary<object, object?>
    {
        if (key == default || !dictionary.TryGetValue(key.Value, out object? property))
        {
            value = default;
            return false;
        }
        
        value = (TProperty)property!;
        return true;
    }
    
    public static void SetProperty<TDictionary, TProperty>(this TDictionary dictionary, DAsyncPropertyKey<TProperty> key, TProperty value)
        where TDictionary : IDictionary<object, object?>
    {
        if (key == default)
            throw new ArgumentException("Invalid key.", nameof(key));

        dictionary[key.Value] = value;
    }
}