namespace DTasks.Extensions.Microsoft.DependencyInjection.Hosting;

internal interface IKeyedServiceToken
{
    object? Key { get; set; }
}
