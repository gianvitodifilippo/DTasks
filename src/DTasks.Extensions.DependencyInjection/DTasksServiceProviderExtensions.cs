using System.Runtime.CompilerServices;
using DTasks.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.DependencyInjection;

public static class DTasksServiceProviderExtensions
{
    [return: DAsyncService]
    public static object? GetDAsyncService(this IServiceProvider provider, Type serviceType)
    {
        ThrowHelper.ThrowIfNull(provider);
        ThrowHelper.ThrowIfNull(serviceType);

        object? service = provider.GetService(serviceType);
        if (service is not null)
        {
            EnsureDAsyncService(provider, serviceType);
        }

        return service;
    }

    [return: DAsyncService]
    public static T? GetDAsyncService<T>(this IServiceProvider provider)
        where T : notnull
    {
        ThrowHelper.ThrowIfNull(provider);

        T? service = provider.GetService<T>();
        if (service is not null)
        {
            EnsureDAsyncService(provider, typeof(T));
        }

        return service;
    }

    [return: DAsyncService]
    public static object GetRequiredDAsyncService(this IServiceProvider provider, Type serviceType)
    {
        ThrowHelper.ThrowIfNull(provider);
        ThrowHelper.ThrowIfNull(serviceType);

        object service = provider.GetRequiredService(serviceType);
        EnsureDAsyncService(provider, serviceType);

        return service;
    }

    [return: DAsyncService]
    public static T GetRequiredDAsyncService<T>(this IServiceProvider provider)
        where T : notnull
    {
        ThrowHelper.ThrowIfNull(provider);

        T service = provider.GetRequiredService<T>();
        EnsureDAsyncService(provider, typeof(T));

        return service;
    }

    [return: DAsyncService]
    public static IEnumerable<object?> GetDAsyncServices(this IServiceProvider provider, Type serviceType)
    {
        ThrowHelper.ThrowIfNull(provider);
        ThrowHelper.ThrowIfNull(serviceType);

        IEnumerable<object?> services = provider.GetServices(serviceType);
        EnsureDAsyncService(provider, serviceType);

        return services;
    }

    [return: DAsyncService]
    public static IEnumerable<T> GetDAsyncServices<T>(this IServiceProvider provider)
        where T : notnull
    {
        ThrowHelper.ThrowIfNull(provider);

        IEnumerable<T> services = provider.GetServices<T>();
        EnsureDAsyncService(provider, typeof(T));

        return services;
    }

    // TODO: provider.GetKeyedService(serviceType, serviceKey) should be available when .NET 10 is shipped (https://github.com/dotnet/runtime/issues/102816)
    [return: DAsyncService]
    public static object? GetKeyedDAsyncService(this IServiceProvider provider, Type serviceType, object? serviceKey)
    {
        ThrowHelper.ThrowIfNull(provider);
        ThrowHelper.ThrowIfNull(serviceType);

        if (provider is not IKeyedServiceProvider keyedServiceProvider)
            throw new InvalidOperationException("This service provider doesn't support keyed services.");

        object? service = keyedServiceProvider.GetKeyedService(serviceType, serviceKey);
        if (service is not null)
        {
            EnsureDAsyncService(provider, serviceType);
        }

        return service;
    }

    [return: DAsyncService]
    public static T? GetKeyedDAsyncService<T>(this IServiceProvider provider, object? serviceKey)
        where T : notnull
    {
        ThrowHelper.ThrowIfNull(provider);

        T? service = provider.GetKeyedService<T>(serviceKey);
        if (service is not null)
        {
            EnsureDAsyncService(provider, typeof(T));
        }

        return service;
    }

    [return: DAsyncService]
    public static object GetRequiredKeyedDAsyncService(this IServiceProvider provider, Type serviceType, object? serviceKey)
    {
        ThrowHelper.ThrowIfNull(provider);
        ThrowHelper.ThrowIfNull(serviceType);

        object service = provider.GetRequiredKeyedService(serviceType, serviceKey);
        EnsureDAsyncService(provider, serviceType);

        return service;
    }

    [return: DAsyncService]
    public static T GetRequiredKeyedDAsyncService<T>(this IServiceProvider provider, object? serviceKey)
        where T : notnull
    {
        ThrowHelper.ThrowIfNull(provider);

        T service = provider.GetRequiredKeyedService<T>(serviceKey);
        EnsureDAsyncService(provider, typeof(T));

        return service;
    }

    [return: DAsyncService]
    public static IEnumerable<object?> GetKeyedDAsyncServices(this IServiceProvider provider, Type serviceType, object? serviceKey)
    {
        ThrowHelper.ThrowIfNull(provider);
        ThrowHelper.ThrowIfNull(serviceType);

        IEnumerable<object?> services = provider.GetKeyedServices(serviceType, serviceKey);
        EnsureDAsyncService(provider, serviceType);

        return services;
    }

    [return: DAsyncService]
    public static IEnumerable<T> GetKeyedDAsyncServices<T>(this IServiceProvider provider, object? serviceKey)
        where T : notnull
    {
        ThrowHelper.ThrowIfNull(provider);

        IEnumerable<T> services = provider.GetKeyedServices<T>(serviceKey);
        EnsureDAsyncService(provider, typeof(T));

        return services;
    }

    private static void EnsureDAsyncService(IServiceProvider provider, Type serviceType, [CallerArgumentExpression(nameof(serviceType))] string? parameterName = null)
    {
        IDAsyncServiceRegister register = provider.GetRequiredService<IDAsyncServiceRegister>();
        if (!register.IsDAsyncService(serviceType))
            throw new ArgumentException($"'{serviceType.Name}' is not a d-async service.", parameterName);
    }
}
