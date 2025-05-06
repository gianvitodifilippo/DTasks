using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using DTasks.Utils;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncFeatureCollection
{
    bool TryGetFeature<TFeature>([MaybeNullWhen(false)] out TFeature feature);
}

public static class DAsyncFeatureCollectionExtensions
{
    public static TFeature? GetFeature<TFeature>(this IDAsyncFeatureCollection collection)
    {
        ThrowHelper.ThrowIfNull(collection);
        
        _ = collection.TryGetFeature(out TFeature? feature);
        return feature;
    }

    public static TFeature GetRequiredFeature<TFeature>(this IDAsyncFeatureCollection collection)
    {
        ThrowHelper.ThrowIfNull(collection);
        
        if (!collection.TryGetFeature(out TFeature? feature) || feature is null)
            throw new DAsyncFeatureNotFoundException(typeof(TFeature));

        return feature;
    }
}