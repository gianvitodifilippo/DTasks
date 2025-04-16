namespace DTasks.Infrastructure.Marshaling;

public interface IMarshalingConfiguration
{
    void RegisterSurrogatableType(Type type);
}
