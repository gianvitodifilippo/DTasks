using System.Collections.Frozen;
using DTasks.Infrastructure.Execution;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncFeatureCollection
{
    private static readonly FrozenSet<Type> s_implementedFeatures = new[]
    {
        typeof(ISuspensionFeature),
        typeof(IMarshalingFeature)
    }.ToFrozenSet();
    
    TFeature? IDAsyncFeatureCollection.GetFeature<TFeature>()
        where TFeature : default
    {
        if (s_implementedFeatures.Contains(typeof(TFeature)))
            return (TFeature)(object)this;

        if (_flowProperties.TryGetProperty(MakeFeatureKey<TFeature?>(), out TFeature? feature))
            return feature;

        return default;
    }

    private static DAsyncPropertyKey<TFeature> MakeFeatureKey<TFeature>()
    {
        return new DAsyncPropertyKey<TFeature>(typeof(TFeature));
    }
}