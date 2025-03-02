namespace DTasks.Marshaling;

public interface ITypeResolverBuilder
{
    TypeId Register(Type type);

    ITypeResolver Build();
}
