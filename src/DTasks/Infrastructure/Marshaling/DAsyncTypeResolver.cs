using System.Collections.Frozen;
using System.ComponentModel;
using System.Diagnostics;
using DTasks.Infrastructure.Generics;

namespace DTasks.Infrastructure.Marshaling;

internal sealed class DAsyncTypeResolver : IDAsyncTypeResolver
{
    private readonly FrozenDictionary<Type, TypeId> _typesToIds;
    private readonly FrozenDictionary<TypeId, ITypeContext> _idsToTypeContexts;

    private DAsyncTypeResolver(FrozenDictionary<Type, TypeId> typesToIds, FrozenDictionary<TypeId, ITypeContext> idsToTypeContexts)
    {
        _typesToIds = typesToIds;
        _idsToTypeContexts = idsToTypeContexts;
    }

    public ITypeContext GetTypeContext(TypeId id)
    {
        if (!_idsToTypeContexts.TryGetValue(id, out ITypeContext? typeContext))
            throw new KeyNotFoundException($"No type was registered with id '{id}'.");
        
        return typeContext;
    }

    public TypeId GetTypeId(Type type)
    {
        if (type.ContainsGenericParameters)
            throw new ArgumentException("Open generic types are not supported.", nameof(type));
        
        if (!_typesToIds.TryGetValue(type, out TypeId typeId))
            throw new KeyNotFoundException($"No id was registered for type '{type.FullName}'.");
        
        return typeId;
    }

    public static DAsyncTypeResolver Create(
        Dictionary<Type, TypeId> typesToIds,
        Dictionary<TypeId, ITypeContext> idsToTypeContexts)
    {
        Debug.Assert(typesToIds.Keys.All(type => !type.ContainsGenericParameters), "Types should not contain generic parameters.");
        
        return new(typesToIds.ToFrozenDictionary(), idsToTypeContexts.ToFrozenDictionary());
    }
}
