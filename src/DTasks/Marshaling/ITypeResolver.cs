namespace DTasks.Marshaling;

public interface ITypeResolver
{
    Type GetType(TypeId id);

    TypeId GetTypeId(Type type);
}
