using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncFlowInitializationContext : IDAsyncFlowContext
{
    void AddProperty<TProperty>(DAsyncFlowPropertyKey<TProperty> key, TProperty value);
    
    void SetFeature<TFeature>(TFeature feature);
}