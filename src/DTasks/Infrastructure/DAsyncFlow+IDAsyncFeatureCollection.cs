using DTasks.Infrastructure.Features;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncFeatureCollection
{
    TFeature? IDAsyncFeatureCollection.GetFeature<TFeature>()
        where TFeature : default
    {
        if (typeof(TFeature) == typeof(IDAsyncSuspensionFeature))
            return (TFeature?)(object)this;

        if (_flowProperties is null)
            return default;

        if (_flowProperties.TryGetProperty(MakeFeatureKey<TFeature?>(), out TFeature? feature))
            return feature;

        return default;
    }

    private static DAsyncPropertyKey<TFeature> MakeFeatureKey<TFeature>()
    {
        return new DAsyncPropertyKey<TFeature>(typeof(TFeature));
    }
}