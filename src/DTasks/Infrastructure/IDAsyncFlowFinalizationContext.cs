using System.ComponentModel;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncFlowFinalizationContext
{
    void RemoveProperty<TProperty>(DAsyncFlowPropertyKey<TProperty> key);
}