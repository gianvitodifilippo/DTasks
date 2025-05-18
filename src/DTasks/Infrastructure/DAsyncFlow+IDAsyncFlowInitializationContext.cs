namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncFlowInitializationContext
{
    void IDAsyncFlowInitializationContext.SetFeature<TFeature>(TFeature? feature)
        where TFeature : default
    {
        _flowProperties ??= [];
        _flowProperties.SetProperty(MakeFeatureKey<TFeature?>(), feature);
    }
}
