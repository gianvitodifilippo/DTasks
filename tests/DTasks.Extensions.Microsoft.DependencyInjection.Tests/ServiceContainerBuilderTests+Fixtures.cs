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
            Type implementationType = typeof(MyService);

            yield return [typeof(IMyService), implementationType];
            yield return [typeof(MyService), implementationType];
        }
    }

    public sealed class ImplementationInstanceDataAttribute : DataAttribute
    {
        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            object implementationInstance = new MyService();

            yield return [typeof(IMyService), implementationInstance];
            yield return [typeof(MyService), implementationInstance];
        }
    }

    private interface IMyService;

    private sealed class MyService : IMyService;
}
