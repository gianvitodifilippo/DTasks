using DTasks.Extensions.DependencyInjection.Mapping;
using DTasks.Extensions.DependencyInjection.Marshaling;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using DTasks.Infrastructure.Marshaling;
using Xunit.Sdk;

namespace DTasks.Extensions.DependencyInjection;

public partial class ServiceContainerBuilderTests
{
    private static Expression<Predicate<ServiceToken>> ServiceToken(TypeId typeId)
    {
        return token => token.TypeId == typeId;
    }

    private static Expression<Predicate<KeyedServiceToken<TKey>>> KeyedServiceToken<TKey>(TypeId typeId, TKey key)
        where TKey : notnull
    {
        return token => token.TypeId == typeId && key.Equals(token.Key);
    }

    private static ScopedServiceProvider BuildScopedServiceProvider(IServiceCollection services)
    {
        IServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true
        });

        return new ScopedServiceProvider(provider.CreateScope());
    }

    private static Expression<Func<Exception, bool>> ServiceProviderException(IKeyedServiceProvider provider, Func<IKeyedServiceProvider, object?> getService)
    {
        Exception providerException = provider.Invoking(getService).Should().Throw<Exception>(because: "this should be an invalid descriptor").Subject.Single();

        return ex =>
            ex.Source == providerException.Source &&
            ex.Message == providerException.Message &&
            ex.TargetSite == providerException.TargetSite;
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

    private sealed class TestServiceCollection : IServiceCollection
    {
        private readonly IServiceCollection _services;

        public TestServiceCollection(IServiceMapper mapper)
        {
            _services = new ServiceCollection();
            _services.AddSingleton(mapper);
        }

        public ServiceDescriptor this[int index]
        {
            get => _services[index];
            set => throw new NotImplementedException();
        }

        public int Count => _services.Count;

        public bool IsReadOnly => _services.IsReadOnly;

        public void Add(ServiceDescriptor item)
        {
            if (item.ServiceType == typeof(IServiceMapper))
                return;

            _services.Add(item);
        }

        public bool Contains(ServiceDescriptor item) => _services.Contains(item);

        public void CopyTo(ServiceDescriptor[] array, int arrayIndex) => _services.CopyTo(array, arrayIndex);

        public IEnumerator<ServiceDescriptor> GetEnumerator() => _services.GetEnumerator();

        public bool Remove(ServiceDescriptor item)
        {
            if (item.ServiceType == typeof(IServiceMapper))
                return false;

            return _services.Remove(item);
        }

        #region Not implemented

        public void Clear() => throw new NotImplementedException();

        public int IndexOf(ServiceDescriptor item) => throw new NotImplementedException();

        public void Insert(int index, ServiceDescriptor item) => throw new NotImplementedException();

        public void RemoveAt(int index) => throw new NotImplementedException();

        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

        #endregion
    }

    private sealed class ScopedServiceProvider(IServiceScope scope) : IKeyedServiceProvider, IDisposable
    {
        private IKeyedServiceProvider Provider => (IKeyedServiceProvider)scope.ServiceProvider;

        public object? GetKeyedService(Type serviceType, object? serviceKey) => Provider.GetKeyedService(serviceType, serviceKey);

        public object GetRequiredKeyedService(Type serviceType, object? serviceKey) => Provider.GetRequiredKeyedService(serviceType, serviceKey);

        public object? GetService(Type serviceType) => Provider.GetService(serviceType);

        public void Dispose() => scope.Dispose();
    }

    private interface IService;

    private class Service : IService;

    private class Dependency1;
    private class Dependency2;
    private class Dependency3;
    private class Dependency4;
    private class NonResolvableDependency;

    private record ServiceWithAllKindsOfDependencies(
        [ServiceKey] string Key,
                                                                                      Dependency1 Dependency1,
        [FromKeyedServices(ServiceWithAllKindsOfDependencies.Dep2Key)] Dependency2 Dependency2,
        [DAsyncService] Dependency3 Dependency3,
        [DAsyncService, FromKeyedServices(ServiceWithAllKindsOfDependencies.Dep4Key)] Dependency4 Dependency4,
                                                                                      NonResolvableDependency? Dependency5 = null,
        [FromKeyedServices(ServiceWithAllKindsOfDependencies.Dep6Key)] NonResolvableDependency? Dependency6 = null,
        [DAsyncService] NonResolvableDependency? Dependency7 = null,
        [DAsyncService, FromKeyedServices(ServiceWithAllKindsOfDependencies.Dep8Key)] NonResolvableDependency? Dependency8 = null)
    {
        public const string Dep2Key = "dep2";
        public const string Dep4Key = "dep4";
        public const string Dep6Key = "dep6";
        public const string Dep8Key = "dep8";
    }

    private record ServiceWithStringServiceKey([ServiceKey] string Key);

    private record ServiceWithDefaultedServiceKey([ServiceKey] string Key = "default");

    private record ServiceWithObjectServiceKey([ServiceKey] object Key);

    private record ServiceWithServiceKeyAsDAsyncService([ServiceKey, DAsyncService] object Key);

    private class UnresolvableService
    {
        public UnresolvableService(Dependency1 dependency1) { }

        public UnresolvableService(Dependency2 dependency2) { }
    }

    private record ServiceWithOneDependency(Dependency1 Dependency1);

    private class ServiceWithPrivateConstructor
    {
        private ServiceWithPrivateConstructor() { }
    }
}
