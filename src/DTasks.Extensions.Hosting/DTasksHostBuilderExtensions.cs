using DTasks.Configuration;
using DTasks.Extensions.Hosting;
using DTasks.Utils;

namespace Microsoft.Extensions.Hosting;

public static class DTasksHostBuilderExtensions
{
    public static IHostBuilder UseDTasks(this IHostBuilder hostBuilder, Action<IHostingDTasksConfigurationBuilder> configure)
    {
        ThrowHelper.ThrowIfNull(hostBuilder);
        ThrowHelper.ThrowIfNull(configure);

        return hostBuilder.UseServiceProviderFactory(context => new DTasksServiceProviderFactory(context, configure));
    }
}