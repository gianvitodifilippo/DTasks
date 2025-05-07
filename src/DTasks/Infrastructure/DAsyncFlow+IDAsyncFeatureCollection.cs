using DTasks.Infrastructure.Features;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncFeatureCollection
{
    TFeature? IDAsyncFeatureCollection.GetFeature<TFeature>()
        where TFeature : default
    {
        if (typeof(TFeature) == typeof(IDAsyncSuspensionFeature))
            return (TFeature?)(object)this;

        if (_features is null)
            return default;

        if (_features.TryGetValue(typeof(TFeature), out object? untypedFeature))
            return untypedFeature is TFeature typedFeature ? typedFeature : default;

        return default;
    }
}