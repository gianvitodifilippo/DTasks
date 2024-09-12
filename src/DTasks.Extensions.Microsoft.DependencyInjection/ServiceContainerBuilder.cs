using DTasks.Extensions.Microsoft.DependencyInjection.Hosting;
using DTasks.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;
using System.Reflection;

namespace DTasks.Extensions.Microsoft.DependencyInjection;

using KeyedServiceFactory = Func<IServiceProvider, object?, object>;
using ServiceFactory = Func<IServiceProvider, object>;

internal sealed class ServiceContainerBuilder(IServiceCollection services)
{
    private readonly MethodInfo _markSingletonMethod = typeof(ServiceMarker).GetRequiredMethod(
        name: nameof(ServiceMarker.MarkSingleton),
        bindingAttr: BindingFlags.Instance | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider), typeof(object), typeof(ServiceToken)]);

    private readonly MethodInfo _markScopedMethod = typeof(ServiceMarker).GetRequiredMethod(
        name: nameof(ServiceMarker.MarkScoped),
        bindingAttr: BindingFlags.Instance | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider), typeof(object), typeof(ServiceToken)]);

    private readonly MethodInfo _markTransientMethod = typeof(ServiceMarker).GetRequiredMethod(
        name: nameof(ServiceMarker.MarkTransient),
        bindingAttr: BindingFlags.Instance | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider), typeof(object), typeof(ServiceToken)]);

    private readonly MethodInfo _getServiceMarkerMethod = typeof(ServiceProviderServiceExtensions).GetRequiredMethod(
        name: nameof(ServiceProviderServiceExtensions.GetRequiredService),
        bindingAttr: BindingFlags.Instance | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider)])
        .MakeGenericMethod(typeof(ServiceMarker));

    private readonly MethodInfo _getRequiredKeyedServiceMethod = typeof(ServiceProviderKeyedServiceExtensions).GetRequiredMethod(
        name: nameof(ServiceProviderKeyedServiceExtensions.GetRequiredKeyedService),
        bindingAttr: BindingFlags.Static | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider), typeof(Type), typeof(object)]);

    private readonly ServiceResolverBuilder _resolverBuilder = new();
    private readonly object _rootScopeKey = new();
    private readonly object _childScopeKey = new();

    public void AddDTaskServices()
    {
        ServiceResolver resolver = _resolverBuilder.BuildServiceResolver();

        services
            .AddSingleton(sp => new ServiceMarker(sp, _rootScopeKey, _childScopeKey))
            .AddKeyedSingleton(_rootScopeKey, (sp, key) => new DTaskScope(sp, resolver, null))
            .AddKeyedScoped(_childScopeKey, (sp, key) => new DTaskScope(sp, resolver, sp.GetRequiredKeyedService<DTaskScope>(_rootScopeKey)))
            .AddScoped<IDTaskScope>(sp => sp.GetRequiredService<DTaskScope>());
    }

    public void Mark(ServiceDescriptor descriptor)
    {
        services.Remove(descriptor);

        ServiceTypeId typeId = _resolverBuilder.AddServiceType(descriptor.ServiceType);
        ServiceToken token = descriptor.IsKeyedService
            ? ServiceToken.Create(typeId, descriptor.ServiceKey)
            : ServiceToken.Create(typeId);

        // Using expressions to build the factory helps reducing the cyclomatic complexity of this method

        MethodInfo markMethod = descriptor.Lifetime switch
        {
            ServiceLifetime.Singleton => _markSingletonMethod,
            ServiceLifetime.Scoped => _markScopedMethod,
            ServiceLifetime.Transient => _markTransientMethod,
            _ => throw new InvalidOperationException($"Invalid service lifetime '{descriptor.Lifetime}'.")
        };

        ParameterExpression servicesParam = Expression.Parameter(typeof(IServiceProvider), "services");
        Expression markedInstanceExpr = MakeMarkedInstanceExpression(descriptor, servicesParam);

        // sp.GetRequiredService<ServiceMarker>().`markMethod`(sp, `markedInstanceExpr`, token)
        Expression body = Expression.Call(
            instance: Expression.Call(
                method: _getServiceMarkerMethod,
                arg0: servicesParam),
            method: markMethod,
            arg0: servicesParam,
            arg1: markedInstanceExpr,
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
        if (descriptor.ImplementationType is Type implementationType)
            return MakeImplementationTypeExpression(descriptor, servicesParam, implementationType);

        if (descriptor.ImplementationInstance is object implementationInstance)
            return MakeImplementationInstanceExpression(implementationInstance);

        if (descriptor.ImplementationFactory is ServiceFactory implementationFactory)
            return MakeImplementationFactoryExpression(servicesParam, implementationFactory);

        if (descriptor.KeyedImplementationType is Type keyedImplementationType)
            return MakeImplementationTypeExpression(descriptor, servicesParam, keyedImplementationType);

        if (descriptor.KeyedImplementationInstance is object keyedImplementationInstance)
            return MakeImplementationInstanceExpression(keyedImplementationInstance);

        if (descriptor.KeyedImplementationFactory is KeyedServiceFactory keyedImplementationFactory)
            return MakeKeyedImplementationFactoryExpression(descriptor.ServiceKey, servicesParam, keyedImplementationFactory);

        throw new InvalidOperationException("Invalid service descriptor.");
    }

    private Expression MakeImplementationTypeExpression(ServiceDescriptor descriptor, ParameterExpression servicesParam, Type implementationType)
    {
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
}