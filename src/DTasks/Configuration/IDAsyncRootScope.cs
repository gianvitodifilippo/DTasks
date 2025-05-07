using System.Collections.Immutable;
using System.ComponentModel;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Configuration;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncRootScope
{
    IDAsyncTypeResolver TypeResolver { get; }

    ImmutableArray<Type> SurrogatableTypes { get; }
}
