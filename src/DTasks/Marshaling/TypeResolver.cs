using System.Collections.Frozen;

namespace DTasks.Marshaling;

internal sealed class TypeResolver(
    FrozenDictionary<Type, TypeId> typesToIds,
    FrozenDictionary<TypeId, Type> idsToTypes) : ITypeResolver
{
    public Type GetType(TypeId id) => idsToTypes[id];

    public TypeId GetTypeId(Type type) => typesToIds[type];
}
