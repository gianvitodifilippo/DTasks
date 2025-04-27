using System.ComponentModel;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncFlowInitializationContext
{
    void AddProperty<TProperty>(DAsyncFlowPropertyKey<TProperty> key, TProperty value);
}