namespace DTasks.AspNetCore.Infrastructure.Hosting;

internal sealed class BackgroundDAsyncHost(IServiceProvider services) : AspNetCoreDAsyncHost
{
    protected override IServiceProvider Services => services;
}