namespace DTasks.Infrastructure.Marshaling;

public interface IDAsyncMarshaller
{
    void RegisterSurrogatableType(Type type);
}
