using System.ComponentModel;
using DTasks.Utils;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncFeatureCollection
{
    TFeature? GetFeature<TFeature>();
}

public static class DAsyncFeatureCollectionExtensions
{
    public static TFeature GetRequiredFeature<TFeature>(this IDAsyncFeatureCollection collection)
    {
        ThrowHelper.ThrowIfNull(collection);
        
        return collection.GetFeature<TFeature>() ?? throw new DAsyncFeatureNotFoundException(typeof(TFeature));
    }
}