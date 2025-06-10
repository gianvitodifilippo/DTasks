using System.ComponentModel;
using DTasks.Infrastructure.Generics;
using DTasks.Utils;

namespace DTasks.Infrastructure.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncTypeResolver
{
    ITypeContext GetTypeContext(TypeId id);

    TypeId GetTypeId(Type type);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public static class DAsyncTypeResolverExtensions
{
    public static Type GetType(this IDAsyncTypeResolver typeResolver, TypeId id)
    {
        ThrowHelper.ThrowIfNull(typeResolver);
        
        return typeResolver.GetTypeContext(id).Type;
    }
}