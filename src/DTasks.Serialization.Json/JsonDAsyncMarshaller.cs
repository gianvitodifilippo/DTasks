using DTasks.Infrastructure.Marshaling;

namespace DTasks.Serialization.Json;

internal class JsonDAsyncMarshaller : IMarshalingConfiguration
{
    public void RegisterSurrogatableType(Type type)
    {
        throw new NotImplementedException();
    }
}
