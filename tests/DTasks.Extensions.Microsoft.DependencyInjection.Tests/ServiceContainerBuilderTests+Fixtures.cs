using DTasks.Extensions.Microsoft.DependencyInjection.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;
using System.Reflection;
using Xunit.Sdk;

namespace DTasks.Extensions.Microsoft.DependencyInjection;

public partial class ServiceContainerBuilderTests
{
    private static Expression<Func<ServiceDescriptor, bool>> Singleton<TService>()
    {
        return descriptor =>
            descriptor.ServiceType == typeof(TService) &&
            descriptor.Lifetime == ServiceLifetime.Singleton;
    }

    private static Expression<Func<ServiceDescriptor, bool>> Scoped<TService>()
    {
        return descriptor =>
            descriptor.ServiceType == typeof(TService) &&
            descriptor.Lifetime == ServiceLifetime.Scoped;
    }

    private static Expression<Predicate<ServiceToken>> ServiceToken(string typeId)
    {
        return token => token.TypeId == typeId;
    }

    private static Expression<Predicate<KeyedServiceToken<TKey>>> KeyedServiceToken<TKey>(string typeId, TKey key)
        where TKey : notnull
    {
        return token => token.TypeId == typeId && key.Equals(token.Key);
    }


    public sealed class ImplementationTypeDataAttribute : DataAttribute
    {
        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            Type implementationType = typeof(Service);

            yield return [typeof(IService), implementationType];
            yield return [typeof(Service), implementationType];
        }
    }

    public sealed class ImplementationInstanceDataAttribute : DataAttribute
    {
        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            object implementationInstance = new Service();

            yield return [typeof(IService), implementationInstance];
            yield return [typeof(Service), implementationInstance];
        }
    }

    private interface IService;

    private class Service : IService;

    private class Dependency1;
    private class Dependency2;
    private class Dependency3;
    private class Dependency4;
    private class NonResolvableDependency;

    private record ServiceWithAllKindsOfDependencies(
        [ServiceKey]                               string Key,
                                                   Dependency1 Dependency1,
        [FromKeyedServices("dep2")]                Dependency2 Dependency2,
        [DAsyncService]                            Dependency3 Dependency3,
        [DAsyncService, FromKeyedServices("dep4")] Dependency4 Dependency4,
                                                   NonResolvableDependency? Dependency5 = null,
        [FromKeyedServices("dep6")]                NonResolvableDependency? Dependency6 = null,
        [DAsyncService]                            NonResolvableDependency? Dependency7 = null,
        [DAsyncService, FromKeyedServices("dep8")] NonResolvableDependency? Dependency8 = null);

    private record ServiceWithInvalidKey([ServiceKey] string Key);
}
