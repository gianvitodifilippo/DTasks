using DTasks.Extensions.Hosting;
using DTasks.Utils;

namespace Microsoft.Extensions.Hosting;

public static class DTasksHostBuilderExtensions
{
    public static IHostBuilder UseDTasks(this IHostBuilder hostBuilder)
    {
        ThrowHelper.ThrowIfNull(hostBuilder);

        return hostBuilder.UseDTasksCore(configuration => { });
    }

    public static IHostBuilder UseDTasks(this IHostBuilder hostBuilder, Action<IDTasksHostBuilderConfiguration> configure)
    {
        ThrowHelper.ThrowIfNull(hostBuilder);
        ThrowHelper.ThrowIfNull(configure);

        return hostBuilder.UseDTasksCore(configure);
    }

    private static IHostBuilder UseDTasksCore(this IHostBuilder hostBuilder, Action<IDTasksHostBuilderConfiguration> configure)
    {
        return hostBuilder.UseServiceProviderFactory(context => new DTasksServiceProviderFactory(context, configure));
    }
}