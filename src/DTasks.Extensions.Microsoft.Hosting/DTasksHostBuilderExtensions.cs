using DTasks.Extensions.Microsoft.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

public static class DTasksHostBuilderExtensions
{
    public static IHostBuilder UseDTasks(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseDTasksCore((context, options) => options);
    }

    public static IHostBuilder UseDTasks(this IHostBuilder hostBuilder, ServiceProviderOptions options)
    {
        return hostBuilder.UseDTasksCore((context, originalOptions) => options);
    }

    public static IHostBuilder UseDTasks(this IHostBuilder hostBuilder, Action<ServiceProviderOptions> configureOptions)
    {
        return hostBuilder.UseDTasksCore((context, originalOptions) =>
        {
            configureOptions(originalOptions);
            return originalOptions;
        });
    }

    public static IHostBuilder UseDTasks(this IHostBuilder hostBuilder, Action<HostBuilderContext, ServiceProviderOptions> configureOptions)
    {
        return hostBuilder.UseDTasksCore((context, originalOptions) =>
        {
            configureOptions(context, originalOptions);
            return originalOptions;
        });
    }

    private static IHostBuilder UseDTasksCore(this IHostBuilder hostBuilder, Func<HostBuilderContext, ServiceProviderOptions, ServiceProviderOptions> configureOptions)
    {
        return hostBuilder.UseServiceProviderFactory(context =>
        {
            ServiceProviderOptions options = configureOptions(context, new ServiceProviderOptions());
            return new DTasksServiceProviderFactory(options);
        });
    }
}
