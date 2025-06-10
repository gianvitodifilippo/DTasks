using System.ComponentModel;

namespace DTasks.Infrastructure.State;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IHydrationContext
{
    DAsyncId Id { get; }
}