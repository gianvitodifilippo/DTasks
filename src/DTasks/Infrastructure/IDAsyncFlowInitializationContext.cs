using System.ComponentModel;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncFlowInitializationContext : IDAsyncFlowContext
{
    void SetFeature<TFeature>(TFeature? feature);
}