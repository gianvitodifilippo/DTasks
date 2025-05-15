using DTasks.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.DependencyInjection.Infrastructure;

internal sealed class DAsyncHostInfrastructureProvider(IServiceProvider rootProvider, object serviceKey) : IDisposable
{
    private readonly IServiceScope _scope = rootProvider.CreateScope();
    
    public IDAsyncHostInfrastructure GetInfrastructure(IServiceProvider provider)
    {
        return provider == rootProvider
            ? _scope.ServiceProvider.GetRequiredKeyedService<IDAsyncHostInfrastructure>(serviceKey)
            : provider.GetRequiredKeyedService<IDAsyncHostInfrastructure>(serviceKey);
    }

    public void Dispose()
    {
        _scope.Dispose();
    }
}