using DTasks.Infrastructure.Marshaling;

namespace DTasks.Extensions.DependencyInjection.Marshaling;

internal class ServiceSurrogate
{
    public TypeId TypeId { get; set; }


    public static ServiceSurrogate Create(TypeId typeId) => new() { TypeId = typeId };

    public static ServiceSurrogate Create(TypeId typeId, object key)
    {
        ServiceSurrogate surrogate = key switch
        {
            string stringKey => new KeyedServiceSurrogate<string>() { Key = stringKey },
            int intKey => new KeyedServiceSurrogate<int>() { Key = intKey },
            _ => throw new ArgumentException($"Keys of type {key.GetType()} are not supported.", nameof(key))
        };

        surrogate.TypeId = typeId;
        return surrogate;
    }
}
