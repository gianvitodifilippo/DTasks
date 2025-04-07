namespace DTasks.Extensions.DependencyInjection.Marshaling;

internal class ServiceToken
{
    public TypeId TypeId { get; set; }


    public static ServiceToken Create(TypeId typeId) => new() { TypeId = typeId };

    public static ServiceToken Create(TypeId typeId, object key)
    {
        ServiceToken token = key switch
        {
            string stringKey => new KeyedServiceToken<string>() { Key = stringKey },
            int intKey => new KeyedServiceToken<int>() { Key = intKey },
            _ => throw new ArgumentException($"Keys of type {key.GetType()} are not supported.", nameof(key))
        };

        token.TypeId = typeId;
        return token;
    }
}
