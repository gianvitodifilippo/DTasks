namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncFlowInitializationContext
{
    void IDAsyncFlowInitializationContext.AddProperty<TProperty>(DAsyncFlowPropertyKey<TProperty> key, TProperty value)
    {
        _properties.Add(key.Key, value);
    }

    void IDAsyncFlowInitializationContext.SetFeature<TFeature>(TFeature feature)
    {
        _properties.Add(MakeFeaturePropertyKey<TFeature>(), feature);
    }
}
