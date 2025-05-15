using System.Diagnostics.CodeAnalysis;
using DTasks.Infrastructure.DependencyInjection;
using DTasks.Infrastructure.State;

namespace DTasks.Infrastructure;

internal sealed class DAsyncHostInfrastructure : IDAsyncHostInfrastructure, IDAsyncHostScope
{
    private readonly IDAsyncInfrastructure _infrastructure;
    private readonly IDAsyncHost _host;
    private readonly HostComponentProvider _hostComponentProvider;

    public DAsyncHostInfrastructure(IDAsyncInfrastructure infrastructure, IDAsyncHost host)
    {
        _infrastructure = infrastructure;
        _host = host;
        _hostComponentProvider = infrastructure.RootProvider.CreateHostProvider(this);
    }
    
    public IDAsyncRootInfrastructure Parent => _infrastructure.RootInfrastructure;

    IDAsyncRootScope IDAsyncHostScope.Parent => _hostComponentProvider.RootScope;
    
    bool IDAsyncHostScope.TryGetProperty<TProperty>(DAsyncPropertyKey<TProperty> key, [MaybeNullWhen(false)] out TProperty value)
    {
        return _host.TryGetProperty(key, out value);
    }

    public IDAsyncHeap GetHeap()
    {
        return _infrastructure.GetHeap(_hostComponentProvider);
    }
}