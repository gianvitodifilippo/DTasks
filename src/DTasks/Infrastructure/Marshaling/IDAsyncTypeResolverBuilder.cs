using System.ComponentModel;

namespace DTasks.Infrastructure.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncTypeResolverBuilder
{
    TypeId Register(Type type);

    IDAsyncTypeResolver Build();
}
