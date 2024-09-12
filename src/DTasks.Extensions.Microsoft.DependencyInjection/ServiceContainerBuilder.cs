using DTasks.Extensions.Microsoft.DependencyInjection.Hosting;
using DTasks.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;
using System.Reflection;

namespace DTasks.Extensions.Microsoft.DependencyInjection;

using KeyedServiceFactory = Func<IServiceProvider, object?, object>;
using ServiceFactory = Func<IServiceProvider, object>;

internal sealed class ServiceContainerBuilder(IServiceCollection services, IServiceResolverBuilder resolverBuilder) : IServiceContainerBuilder
{
    private readonly MethodInfo _mapSingletonMethod = typeof(ILifetimeServiceMapper).GetRequiredMethod(
        name: nameof(ILifetimeServiceMapper.MapSingleton),
        bindingAttr: BindingFlags.Instance | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider), typeof(object), typeof(ServiceToken)]);

    private readonly MethodInfo _mapScopedMethod = typeof(ILifetimeServiceMapper).GetRequiredMethod(
        name: nameof(ILifetimeServiceMapper.MapScoped),
        bindingAttr: BindingFlags.Instance | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider), typeof(object), typeof(ServiceToken)]);

    private readonly MethodInfo _mapTransientMethod = typeof(ILifetimeServiceMapper).GetRequiredMethod(
        name: nameof(ILifetimeServiceMapper.MapTransient),
        bindingAttr: BindingFlags.Instance | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider), typeof(object), typeof(ServiceToken)]);

    private readonly MethodInfo _getServiceMapperMethod = typeof(ServiceProviderServiceExtensions).GetRequiredMethod(
        name: nameof(ServiceProviderServiceExtensions.GetRequiredService),
        bindingAttr: BindingFlags.Static | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider)])
        .MakeGenericMethod(typeof(ILifetimeServiceMapper));

    private readonly MethodInfo _getRequiredKeyedServiceMethod = typeof(ServiceProviderKeyedServiceExtensions).GetRequiredMethod(
        name: nameof(ServiceProviderKeyedServiceExtensions.GetRequiredKeyedService),
        bindingAttr: BindingFlags.Static | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider), typeof(Type), typeof(object)]);

    public void AddDTaskServices()
    {
        ServiceResolver resolver = resolverBuilder.BuildServiceResolver();

        services
            .AddSingleton(resolver)
            .AddSingleton<ILifetimeServiceMapper, LifetimeServiceMapper>()
            .AddSingleton<RootDTaskScope>()
            .AddSingleton<IRootDTaskScope>(sp => sp.GetRequiredService<RootDTaskScope>())
            .AddSingleton<IRootServiceMapper>(sp => sp.GetRequiredService<RootDTaskScope>())
            .AddScoped(sp => new ChildDTaskScope(sp, sp.GetRequiredService<ServiceResolver>(), sp.GetRequiredService<RootDTaskScope>()))
            .AddScoped<IDTaskScope>(sp => sp.GetRequiredService<ChildDTaskScope>())
            .AddScoped<IServiceMapper>(sp => sp.GetRequiredService<ChildDTaskScope>());
    }

    public void Intercept(ServiceDescriptor descriptor)
    {
        services.Remove(descriptor);

        ServiceTypeId typeId = resolverBuilder.AddServiceType(descriptor.ServiceType);
        ServiceToken token = descriptor.IsKeyedService
            ? ServiceToken.Create(typeId, descriptor.ServiceKey)
            : ServiceToken.Create(typeId);

        // Using expressions to build the factory helps reducing the cyclomatic complexity of this method

        MethodInfo mapMethod = descriptor.Lifetime switch
        {
            ServiceLifetime.Singleton => _mapSingletonMethod,
            ServiceLifetime.Scoped => _mapScopedMethod,
            ServiceLifetime.Transient => _mapTransientMethod,
            _ => throw new InvalidOperationException($"Invalid service lifetime '{descriptor.Lifetime}'.")
        };

        ParameterExpression servicesParam = Expression.Parameter(typeof(IServiceProvider), "services");
        Expression mappdInstanceExpr = MakeMarkedInstanceExpression(descriptor, servicesParam);

        // sp.GetRequiredService<ILifetimeServiceMapper>().`mapMethod`(sp, `mappdInstanceExpr`, token)
        Expression body = Expression.Call(
            instance: Expression.Call(
                method: _getServiceMapperMethod,
                arg0: servicesParam),
            method: mapMethod,
            arg0: servicesParam,
            arg1: mappdInstanceExpr,
            arg2: Expression.Constant(token));

        if (descriptor.IsKeyedService)
        {
            ParameterExpression keyParam = Expression.Parameter(typeof(object), "key");
            Expression<KeyedServiceFactory> factoryLambda = Expression.Lambda<KeyedServiceFactory>(body, [servicesParam, keyParam]);
            KeyedServiceFactory factory = factoryLambda.Compile();

            services.Add(new ServiceDescriptor(descriptor.ServiceType, descriptor.ServiceKey, factory, descriptor.Lifetime));
        }
        else
        {
            Expression<ServiceFactory> factoryLambda = Expression.Lambda<ServiceFactory>(body, [servicesParam]);
            ServiceFactory factory = factoryLambda.Compile();

            services.Add(new ServiceDescriptor(descriptor.ServiceType, factory, descriptor.Lifetime));
        }
    }

    private Expression MakeMarkedInstanceExpression(ServiceDescriptor descriptor, ParameterExpression servicesParam)
    {
        if (descriptor.IsKeyedService)
        {
            if (descriptor.KeyedImplementationType is Type keyedImplementationType)
                return MakeImplementationTypeExpression(descriptor, servicesParam, keyedImplementationType);

            if (descriptor.KeyedImplementationInstance is object keyedImplementationInstance)
                return MakeImplementationInstanceExpression(keyedImplementationInstance);

            if (descriptor.KeyedImplementationFactory is KeyedServiceFactory keyedImplementationFactory)
                return MakeKeyedImplementationFactoryExpression(descriptor.ServiceKey, servicesParam, keyedImplementationFactory);
        }
        else
        {
            if (descriptor.ImplementationType is Type implementationType)
                return MakeImplementationTypeExpression(descriptor, servicesParam, implementationType);

            if (descriptor.ImplementationInstance is object implementationInstance)
                return MakeImplementationInstanceExpression(implementationInstance);

            if (descriptor.ImplementationFactory is ServiceFactory implementationFactory)
                return MakeImplementationFactoryExpression(servicesParam, implementationFactory);
        }

        throw new InvalidOperationException("Invalid service descriptor.");
    }

    private Expression MakeImplementationTypeExpression(ServiceDescriptor descriptor, ParameterExpression servicesParam, Type implementationType)
    {
        // We register the original service descriptor with a key that is only visible here.
        // We don't register the implementation type directly for two reasons:
        // 1. When descriptor.ServiceType == descriptor.ImplementationType we register the same service type twice, and
        // 2. We don't want to add an extra service which is publicly resolvable by the consumers.

        object helperKey = new();
        services.Add(new ServiceDescriptor(descriptor.ServiceType, helperKey, implementationType, descriptor.Lifetime));

        // sp.GetRequiredKeyedService(descriptor.ServiceType, helperKey)
        return Expression.Call(
            method: _getRequiredKeyedServiceMethod,
            arg0: servicesParam,
            arg1: Expression.Constant(descriptor.ServiceType),
            arg2: Expression.Constant(helperKey));
    }

    private static Expression MakeImplementationInstanceExpression(object implementationInstance)
    {
        // implementationInstance
        return Expression.Constant(implementationInstance);
    }

    private static Expression MakeImplementationFactoryExpression(ParameterExpression servicesParam, ServiceFactory implementationFactory)
    {
        // implementationFactory(services)
        return Expression.Invoke(
            expression: Expression.Constant(implementationFactory),
            arguments: servicesParam);
    }

    private static Expression MakeKeyedImplementationFactoryExpression(object? serviceKey, ParameterExpression servicesParam, KeyedServiceFactory keyedImplementationFactory)
    {
        // keyedImplementationFactory(services, serviceKey)
        return Expression.Invoke(
            expression: Expression.Constant(keyedImplementationFactory),
            arguments: [servicesParam, Expression.Constant(serviceKey)]);
    }

    public static ServiceContainerBuilder Create(IServiceCollection services)
    {
        return new ServiceContainerBuilder(services, new ServiceResolverBuilder());
    }
}