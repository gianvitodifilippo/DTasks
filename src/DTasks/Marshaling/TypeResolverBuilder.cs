using DTasks.Hosting;
using DTasks.Inspection;
using DTasks.Utils;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DTasks.Marshaling;

public sealed class TypeResolverBuilder : ITypeResolverBuilder, ITypeResolver
{
    private readonly Dictionary<Type, TypeId> _typesToIds = [];
    private readonly Dictionary<TypeId, Type> _idsToTypes = [];

    private TypeResolverBuilder() { }

    public TypeId Register(Type type)
    {
        ThrowHelper.ThrowIfNull(type);

        if (type.ContainsGenericParameters)
            throw new ArgumentException("Open generic types are not supported.", nameof(type));

        if (_typesToIds.TryGetValue(type, out TypeId id))
            return id;

        int count = _typesToIds.Count + 1;
        id = new(count.ToString()); // Naive

        _typesToIds.Add(type, id);
        _idsToTypes.Add(id, type);

        Debug.Assert(_typesToIds.Count == count && _idsToTypes.Count == count);

        return id;
    }

    public ITypeResolver Build()
    {
        return new TypeResolver(_typesToIds.ToFrozenDictionary(), _idsToTypes.ToFrozenDictionary());
    }

    public Type GetType(TypeId id)
    {
        return _idsToTypes[id];
    }

    public TypeId GetTypeId(Type type)
    {
        return _typesToIds[type];
    }

    public static TypeResolverBuilder CreateDefault()
    {
        TypeResolverBuilder builder = new();
        DAsyncFlow.RegisterTypeIds(builder);

        return builder;
    }
}
