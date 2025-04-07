using System.ComponentModel;

namespace DTasks.Infrastructure.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncTypeResolver
{
    Type GetType(TypeId id);

    TypeId GetTypeId(Type type);
}
