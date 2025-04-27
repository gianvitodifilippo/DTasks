using DTasks.Configuration;
using DTasks.Utils;
using Microsoft.Extensions.Hosting;

namespace DTasks.Extensions.Hosting;

public static class DTasksHostBuilderExtensions
{
    public static IHostBuilder UseDTasks(this IHostBuilder hostBuilder, Action<IHostingDTasksConfigurationBuilder> configure)
    {
        ThrowHelper.ThrowIfNull(hostBuilder);
        ThrowHelper.ThrowIfNull(configure);

        return hostBuilder.UseServiceProviderFactory(context => new DTasksServiceProviderFactory(context, configure));
    }
}