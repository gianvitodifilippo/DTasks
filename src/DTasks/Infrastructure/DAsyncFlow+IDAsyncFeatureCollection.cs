using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using DTasks.Infrastructure.Features;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncFeatureCollection
{
    bool IDAsyncFeatureCollection.TryGetFeature<TFeature>([MaybeNullWhen(false)] out TFeature feature)
    {
        if (typeof(TFeature) == typeof(IDAsyncSuspensionFeature))
        {
            feature = (TFeature)(object)this;
            return true;
        }
        
        return TryGetProperty(MakeFeaturePropertyKey<TFeature>(), out feature);
    }

    private DAsyncFlowPropertyKey<TFeature> MakeFeaturePropertyKey<TFeature>()
    {
        return new DAsyncFlowPropertyKey<TFeature>(typeof(FeatureType<TFeature>));
    }

    private static class FeatureType<TFeature>;
}