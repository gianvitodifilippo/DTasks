using System.Collections.Frozen;
using System.ComponentModel;

namespace DTasks.Infrastructure.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed class DAsyncTypeResolver(
    FrozenDictionary<Type, TypeId> typesToIds,
    FrozenDictionary<TypeId, Type> idsToTypes) : IDAsyncTypeResolver
{
    public Type GetType(TypeId id) => idsToTypes[id];

    public TypeId GetTypeId(Type type) => typesToIds[type];
}
