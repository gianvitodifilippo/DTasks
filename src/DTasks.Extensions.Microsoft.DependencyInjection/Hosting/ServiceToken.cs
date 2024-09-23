namespace DTasks.Extensions.Microsoft.DependencyInjection.Hosting;

internal class ServiceToken
{
    protected ServiceToken() { }

    public string? TypeId { get; set; }


    public static ServiceToken Create(ServiceTypeId typeId) => new() { TypeId = typeId.ToString() };

    public static ServiceToken Create(ServiceTypeId typeId, object? key)
    {
        Type keyType = key?.GetType() ?? typeof(object);
        Type tokenType = typeof(KeyedServiceToken<>).MakeGenericType(keyType);
        ServiceToken token = (ServiceToken)Activator.CreateInstance(tokenType, nonPublic: true)!;

        token.TypeId = typeId.ToString();
        ((IKeyedServiceToken)token).Key = key;

        return token;
    }
}
