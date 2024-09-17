using DTasks.Extensions.Microsoft.DependencyInjection.CodeAnalysis;
using DTasks.Utils;

namespace DTasks.Extensions.Microsoft.DependencyInjection;

public static class DTasksServiceProviderExtensions
{
    [return: DAsyncService]
    public static object? GetDAsyncService(this IServiceProvider provider, Type serviceType)
    {
        ThrowHelper.ThrowIfNull(provider);
        ThrowHelper.ThrowIfNull(serviceType);

        throw new NotImplementedException();
    }

    [return: DAsyncService]
    public static T? GetDAsyncService<T>(this IServiceProvider provider)
        where T : notnull
    {
        ThrowHelper.ThrowIfNull(provider);

        throw new NotImplementedException();
    }

    [return: DAsyncService]
    public static object GetRequiredDAsyncService(this IServiceProvider provider, Type serviceType)
    {
        ThrowHelper.ThrowIfNull(provider);
        ThrowHelper.ThrowIfNull(serviceType);

        throw new NotImplementedException();
    }

    [return: DAsyncService]
    public static T GetRequiredDAsyncService<T>(this IServiceProvider provider)
        where T : notnull
    {
        ThrowHelper.ThrowIfNull(provider);

        throw new NotImplementedException();
    }

    [return: DAsyncService]
    public static IEnumerable<object?> GetDAsyncServices(this IServiceProvider provider, Type serviceType)
    {
        ThrowHelper.ThrowIfNull(provider);
        ThrowHelper.ThrowIfNull(serviceType);

        throw new NotImplementedException();
    }

    [return: DAsyncService]
    public static IEnumerable<T> GetDAsyncServices<T>(this IServiceProvider provider)
        where T : notnull
    {
        ThrowHelper.ThrowIfNull(provider);

        throw new NotImplementedException();
    }

    [return: DAsyncService]
    public static object? GetKeyedDAsyncService(this IServiceProvider provider, Type serviceType, object? serviceKey)
    {
        ThrowHelper.ThrowIfNull(provider);
        ThrowHelper.ThrowIfNull(serviceType);

        throw new NotImplementedException();
    }

    [return: DAsyncService]
    public static T? GetKeyedDAsyncService<T>(this IServiceProvider provider, object? serviceKey)
    {
        ThrowHelper.ThrowIfNull(provider);

        throw new NotImplementedException();
    }

    [return: DAsyncService]
    public static object GetRequiredKeyedService(this IServiceProvider provider, Type serviceType, object? serviceKey)
    {
        ThrowHelper.ThrowIfNull(provider);
        ThrowHelper.ThrowIfNull(serviceType);

        throw new NotImplementedException();
    }

    [return: DAsyncService]
    public static T GetRequiredKeyedService<T>(this IServiceProvider provider, object? serviceKey) where T : notnull
    {
        ThrowHelper.ThrowIfNull(provider);

        throw new NotImplementedException();
    }

    [return: DAsyncService]
    public static IEnumerable<object?> GetKeyedDAsyncServices(this IServiceProvider provider, Type serviceType, object? serviceKey)
    {
        ThrowHelper.ThrowIfNull(provider);
        ThrowHelper.ThrowIfNull(serviceType);

        throw new NotImplementedException();
    }

    [return: DAsyncService]
    public static IEnumerable<T> GetKeyedDAsyncServices<T>(this IServiceProvider provider, object? serviceKey)
    {
        ThrowHelper.ThrowIfNull(provider);

        throw new NotImplementedException();
    }
}
