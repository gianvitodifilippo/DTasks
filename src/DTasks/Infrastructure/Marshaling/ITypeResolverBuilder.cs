using System.ComponentModel;

namespace DTasks.Infrastructure.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface ITypeResolverBuilder
{
    TypeId Register(Type type);

    IDAsyncTypeResolver Build();
}
