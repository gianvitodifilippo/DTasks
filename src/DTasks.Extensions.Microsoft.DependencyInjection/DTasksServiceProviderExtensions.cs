using DTasks.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace DTasks.Extensions.Microsoft.DependencyInjection;

public static class DTasksServiceProviderExtensions
{
    [return: DAsyncService]
    public static object? GetDAsyncService(this IServiceProvider provider, Type serviceType)
    {
        ThrowHelper.ThrowIfNull(provider);
        ThrowHelper.ThrowIfNull(serviceType);
        EnsureDAsyncService(provider, serviceType);

        return provider.GetService(serviceType);
    }

    [return: DAsyncService]
    public static T? GetDAsyncService<T>(this IServiceProvider provider)
        where T : notnull
    {
        ThrowHelper.ThrowIfNull(provider);
        EnsureDAsyncService(provider, typeof(T));

        return provider.GetService<T>();
    }

    [return: DAsyncService]
    public static object GetRequiredDAsyncService(this IServiceProvider provider, Type serviceType)
    {
        ThrowHelper.ThrowIfNull(provider);
        ThrowHelper.ThrowIfNull(serviceType);
        EnsureDAsyncService(provider, serviceType);

        return provider.GetRequiredService(serviceType);
    }

    [return: DAsyncService]
    public static T GetRequiredDAsyncService<T>(this IServiceProvider provider)
        where T : notnull
    {
        ThrowHelper.ThrowIfNull(provider);
        EnsureDAsyncService(provider, typeof(T));

        return provider.GetRequiredService<T>();
    }

    [return: DAsyncService]
    public static IEnumerable<object?> GetDAsyncServices(this IServiceProvider provider, Type serviceType)
    {
        ThrowHelper.ThrowIfNull(provider);
        ThrowHelper.ThrowIfNull(serviceType);
        EnsureDAsyncService(provider, serviceType);

        return provider.GetServices(serviceType);
    }

    [return: DAsyncService]
    public static IEnumerable<T> GetDAsyncServices<T>(this IServiceProvider provider)
        where T : notnull
    {
        ThrowHelper.ThrowIfNull(provider);
        EnsureDAsyncService(provider, typeof(T));

        return provider.GetServices<T>();
    }

    [return: DAsyncService]
    public static object? GetKeyedDAsyncService(this IServiceProvider provider, Type serviceType, object? serviceKey)
    {
        ThrowHelper.ThrowIfNull(provider);
        ThrowHelper.ThrowIfNull(serviceType);
        EnsureDAsyncService(provider, serviceType);

        // TODO: This provider.GetKeyedService(serviceType, serviceKey) should be available when .NET 9 is shipped (https://github.com/dotnet/runtime/issues/102816)
        if (provider is IKeyedServiceProvider keyedServiceProvider)
        {
            return keyedServiceProvider.GetKeyedService(serviceType, serviceKey);
        }

        throw new InvalidOperationException("This service provider doesn't support keyed services.");
    }

    [return: DAsyncService]
    public static T? GetKeyedDAsyncService<T>(this IServiceProvider provider, object? serviceKey)
        where T : notnull
    {
        ThrowHelper.ThrowIfNull(provider);
        EnsureDAsyncService(provider, typeof(T));

        return provider.GetKeyedService<T>(serviceKey);
    }

    [return: DAsyncService]
    public static object GetRequiredKeyedDAsyncService(this IServiceProvider provider, Type serviceType, object? serviceKey)
    {
        ThrowHelper.ThrowIfNull(provider);
        ThrowHelper.ThrowIfNull(serviceType);
        EnsureDAsyncService(provider, serviceType);

        return provider.GetRequiredKeyedService(serviceType, serviceKey);
    }

    [return: DAsyncService]
    public static T GetRequiredKeyedDAsyncService<T>(this IServiceProvider provider, object? serviceKey)
        where T : notnull
    {
        ThrowHelper.ThrowIfNull(provider);
        EnsureDAsyncService(provider, typeof(T));

        return provider.GetRequiredKeyedService<T>(serviceKey);
    }

    [return: DAsyncService]
    public static IEnumerable<object?> GetKeyedDAsyncServices(this IServiceProvider provider, Type serviceType, object? serviceKey)
    {
        ThrowHelper.ThrowIfNull(provider);
        ThrowHelper.ThrowIfNull(serviceType);
        EnsureDAsyncService(provider, serviceType);

        return provider.GetKeyedServices(serviceType, serviceKey);
    }

    [return: DAsyncService]
    public static IEnumerable<T> GetKeyedDAsyncServices<T>(this IServiceProvider provider, object? serviceKey)
        where T : notnull
    {
        ThrowHelper.ThrowIfNull(provider);
        EnsureDAsyncService(provider, typeof(T));

        return provider.GetKeyedServices<T>(serviceKey);
    }

    private static void EnsureDAsyncService(IServiceProvider provider, Type serviceType, [CallerArgumentExpression(nameof(serviceType))] string? parameterName = null)
    {
        IServiceRegister register = provider.GetRequiredService<IServiceRegister>();
        if (!register.IsDAsyncService(serviceType))
            throw new ArgumentException($"'{serviceType.Name}' is not a d-async service.", parameterName);
    }
}
