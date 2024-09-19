using DTasks.Extensions.Microsoft.DependencyInjection.Hosting;
using DTasks.Extensions.Microsoft.DependencyInjection.Mapping;
using DTasks.Extensions.Microsoft.DependencyInjection.Utils;
using DTasks.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace DTasks.Extensions.Microsoft.DependencyInjection;

using KeyedServiceFactory = Func<IServiceProvider, object?, object>;
using ServiceFactory = Func<IServiceProvider, object>;

internal sealed class ServiceContainerBuilder(IServiceCollection services, IServiceRegisterBuilder registerBuilder) : IServiceContainerBuilder
{
    private readonly MethodInfo _mapSingletonMethod = typeof(IServiceMapper).GetRequiredMethod(
        name: nameof(IServiceMapper.MapSingleton),
        bindingAttr: BindingFlags.Instance | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider), typeof(object), typeof(ServiceToken)]);

    private readonly MethodInfo _mapScopedMethod = typeof(IServiceMapper).GetRequiredMethod(
        name: nameof(IServiceMapper.MapScoped),
        bindingAttr: BindingFlags.Instance | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider), typeof(object), typeof(ServiceToken)]);

    private readonly MethodInfo _mapTransientMethod = typeof(IServiceMapper).GetRequiredMethod(
        name: nameof(IServiceMapper.MapTransient),
        bindingAttr: BindingFlags.Instance | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider), typeof(object), typeof(ServiceToken)]);

    private readonly MethodInfo _getServiceMapperMethod = typeof(ServiceProviderServiceExtensions).GetRequiredMethod(
        name: nameof(ServiceProviderServiceExtensions.GetRequiredService),
        bindingAttr: BindingFlags.Static | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider)])
        .MakeGenericMethod(typeof(IServiceMapper));

    private readonly MethodInfo _getServiceGenericMethod = typeof(ServiceProviderServiceExtensions).GetRequiredMethod(
        name: nameof(ServiceProviderServiceExtensions.GetService),
        bindingAttr: BindingFlags.Static | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider)]);

    private readonly MethodInfo _getKeyedServiceGenericMethod = typeof(ServiceProviderKeyedServiceExtensions).GetRequiredMethod(
        name: nameof(ServiceProviderKeyedServiceExtensions.GetKeyedService),
        bindingAttr: BindingFlags.Static | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider), typeof(object)]);

    private readonly MethodInfo _getDAsyncServiceGenericMethod = typeof(DTasksServiceProviderExtensions).GetRequiredMethod(
        name: nameof(DTasksServiceProviderExtensions.GetDAsyncService),
        bindingAttr: BindingFlags.Static | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider)]);

    private readonly MethodInfo _getKeyedDAsyncServiceGenericMethod = typeof(DTasksServiceProviderExtensions).GetRequiredMethod(
        name: nameof(DTasksServiceProviderExtensions.GetKeyedDAsyncService),
        bindingAttr: BindingFlags.Static | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider), typeof(object)]);

    private readonly MethodInfo _getRequiredServiceGenericMethod = typeof(ServiceProviderServiceExtensions).GetRequiredMethod(
        name: nameof(ServiceProviderServiceExtensions.GetRequiredService),
        bindingAttr: BindingFlags.Static | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider)]);

    private readonly MethodInfo _getRequiredKeyedServiceGenericMethod = typeof(ServiceProviderKeyedServiceExtensions).GetRequiredMethod(
        name: nameof(ServiceProviderKeyedServiceExtensions.GetRequiredKeyedService),
        bindingAttr: BindingFlags.Static | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider), typeof(object)]);

    private readonly MethodInfo _getRequiredDAsyncServiceGenericMethod = typeof(DTasksServiceProviderExtensions).GetRequiredMethod(
        name: nameof(DTasksServiceProviderExtensions.GetRequiredDAsyncService),
        bindingAttr: BindingFlags.Static | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider)]);

    private readonly MethodInfo _getRequiredKeyedDAsyncServiceGenericMethod = typeof(DTasksServiceProviderExtensions).GetRequiredMethod(
        name: nameof(DTasksServiceProviderExtensions.GetRequiredKeyedDAsyncService),
        bindingAttr: BindingFlags.Static | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider), typeof(object)]);

    private readonly ConstructorLocator _constructorLocator = new(services);

    public void AddDTaskServices()
    {
        IServiceRegister register = registerBuilder.Build();

        services
            .AddSingleton(register)
            .AddSingleton<IServiceMapper, ServiceMapper>()
            .AddSingleton<RootDTaskScope>()
            .AddSingleton<IRootDTaskScope>(provider => provider.GetRequiredService<RootDTaskScope>())
            .AddSingleton<IRootServiceMapper>(provider => provider.GetRequiredService<RootDTaskScope>())
            .AddScoped<ChildDTaskScope>()
            .AddScoped<IDTaskScope>(provider => provider.GetRequiredService<ChildDTaskScope>())
            .AddScoped<IChildServiceMapper>(provider => provider.GetRequiredService<ChildDTaskScope>());
    }

    public void Replace(ServiceDescriptor descriptor)
    {
        Debug.Assert(!descriptor.ServiceType.ContainsGenericParameters, "Open generic services can't be replaced.");

        ServiceTypeId typeId = registerBuilder.AddServiceType(descriptor.ServiceType);
        ServiceToken token = descriptor.IsKeyedService
            ? ServiceToken.Create(typeId, descriptor.ServiceKey)
            : ServiceToken.Create(typeId);

        // Using expressions to build the factory helps reducing the cyclomatic complexity of this method

        MethodInfo mapMethod = descriptor.Lifetime switch
        {
            ServiceLifetime.Singleton => _mapSingletonMethod,
            ServiceLifetime.Scoped => _mapScopedMethod,
            ServiceLifetime.Transient => _mapTransientMethod,
            _ => throw new InvalidOperationException($"Invalid service lifetime: '{descriptor.Lifetime}'.")
        };

        ParameterExpression providerParam = Expression.Parameter(typeof(IServiceProvider), "provider");
        Expression mappedInstanceExpr = MakeMappedInstanceExpression(descriptor, providerParam);

        // provider.GetRequiredService<IServiceMapper>().`mapMethod`(provider, `mappedInstanceExpr`, token)
        Expression body = Expression.Call(
            instance: Expression.Call(
                method: _getServiceMapperMethod,
                arg0: providerParam),
            method: mapMethod,
            arg0: providerParam,
            arg1: mappedInstanceExpr,
            arg2: Expression.Constant(token));

        if (descriptor.IsKeyedService)
        {
            ParameterExpression keyParam = Expression.Parameter(typeof(object), "key");
            Expression<KeyedServiceFactory> factoryLambda = Expression.Lambda<KeyedServiceFactory>(body, [providerParam, keyParam]);
            KeyedServiceFactory factory = factoryLambda.Compile();

            services.Add(new ServiceDescriptor(descriptor.ServiceType, descriptor.ServiceKey, factory, descriptor.Lifetime));
        }
        else
        {
            Expression<ServiceFactory> factoryLambda = Expression.Lambda<ServiceFactory>(body, [providerParam]);
            ServiceFactory factory = factoryLambda.Compile();

            services.Add(new ServiceDescriptor(descriptor.ServiceType, factory, descriptor.Lifetime));
        }

        services.Remove(descriptor);
    }

    private Expression MakeMappedInstanceExpression(ServiceDescriptor descriptor, ParameterExpression providerParam)
    {
        if (descriptor.IsKeyedService)
        {
            if (descriptor.KeyedImplementationType is Type keyedImplementationType)
                return MakeImplementationTypeExpression(descriptor, providerParam, keyedImplementationType);

            if (descriptor.KeyedImplementationInstance is object keyedImplementationInstance)
                return MakeImplementationInstanceExpression(keyedImplementationInstance);

            if (descriptor.KeyedImplementationFactory is KeyedServiceFactory keyedImplementationFactory)
                return MakeKeyedImplementationFactoryExpression(descriptor.ServiceKey, providerParam, keyedImplementationFactory);
        }
        else
        {
            if (descriptor.ImplementationType is Type implementationType)
                return MakeImplementationTypeExpression(descriptor, providerParam, implementationType);

            if (descriptor.ImplementationInstance is object implementationInstance)
                return MakeImplementationInstanceExpression(implementationInstance);

            if (descriptor.ImplementationFactory is ServiceFactory implementationFactory)
                return MakeImplementationFactoryExpression(providerParam, implementationFactory);
        }

        throw new InvalidOperationException("Invalid service descriptor.");
    }

    private Expression MakeImplementationTypeExpression(ServiceDescriptor descriptor, ParameterExpression providerParam, Type implementationType)
    {
        ConstructorInfo? constructor = _constructorLocator.GetDependencyInjectionConstructor(implementationType);
        if (constructor is null)
            throw new NotSupportedException($"Unable to activate type '{implementationType.Name}' as a d-async service.");

        ParameterInfo[] parameters = constructor.GetParameters();
        Expression[] arguments = new Expression[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            ParameterInfo parameter = parameters[i];
            bool isDAsyncDependency = parameter.IsDefined(typeof(DAsyncServiceAttribute), inherit: true);
            bool hasDefaultValue = ParameterDefaultValue.TryGetDefaultValue(parameter, out object? defaultValue);

            if (parameter.IsDefined(typeof(ServiceKeyAttribute), inherit: true))
            {
                if (isDAsyncDependency)
                    throw new InvalidOperationException($"""
                        A constructor parameter may not be decorated with both [ServiceKey] and [DAsyncService].
                        The implementation type that contains this parameter is '{implementationType.Name}'.
                        """);

                object? serviceKey = descriptor.IsKeyedService ? descriptor.ServiceKey : null; // Don't throw here if the service is not keyed, since an exception will be thrown anyway
                arguments[i] = Expression.Constant(serviceKey, parameter.ParameterType);
                break;
            }

            if (parameter.GetCustomAttribute<FromKeyedServicesAttribute>(inherit: true) is { } fromKeyedServiceAttribute)
            {
                if (hasDefaultValue)
                {
                    MethodInfo getDependencyMethod = isDAsyncDependency
                        ? _getKeyedDAsyncServiceGenericMethod.MakeGenericMethod(parameter.ParameterType)
                        : _getKeyedServiceGenericMethod.MakeGenericMethod(parameter.ParameterType);

                    arguments[i] = Expression.Coalesce(
                        left: Expression.Call(
                            method: getDependencyMethod,
                            arg0: providerParam,
                            arg1: Expression.Constant(fromKeyedServiceAttribute.Key)),
                        right: Expression.Constant(defaultValue, parameter.ParameterType));
                }
                else
                {
                    MethodInfo getDependencyMethod = isDAsyncDependency
                        ? _getRequiredKeyedDAsyncServiceGenericMethod.MakeGenericMethod(parameter.ParameterType)
                        : _getRequiredKeyedServiceGenericMethod.MakeGenericMethod(parameter.ParameterType);

                    arguments[i] = Expression.Call(
                        method: getDependencyMethod,
                        arg0: providerParam,
                        arg1: Expression.Constant(fromKeyedServiceAttribute.Key));
                }
            }
            else
            {
                if (hasDefaultValue)
                {
                    MethodInfo getDependencyMethod = isDAsyncDependency
                        ? _getDAsyncServiceGenericMethod.MakeGenericMethod(parameter.ParameterType)
                        : _getServiceGenericMethod.MakeGenericMethod(parameter.ParameterType);

                    arguments[i] = Expression.Coalesce(
                        left: Expression.Call(
                            method: getDependencyMethod,
                            arg0: providerParam),
                        right: Expression.Constant(defaultValue, parameter.ParameterType));
                }
                else
                {
                    MethodInfo getDependencyMethod = isDAsyncDependency
                        ? _getRequiredDAsyncServiceGenericMethod.MakeGenericMethod(parameter.ParameterType)
                        : _getRequiredServiceGenericMethod.MakeGenericMethod(parameter.ParameterType);

                    arguments[i] = Expression.Call(
                        method: getDependencyMethod,
                        arg0: providerParam);
                }
            }
        }

        // new `implementationType`(`arguments`)
        return Expression.New(constructor, arguments);
    }

    private static Expression MakeImplementationInstanceExpression(object implementationInstance)
    {
        // implementationInstance
        return Expression.Constant(implementationInstance);
    }

    private static Expression MakeImplementationFactoryExpression(ParameterExpression providerParam, ServiceFactory implementationFactory)
    {
        // implementationFactory(provider)
        return Expression.Invoke(
            expression: Expression.Constant(implementationFactory),
            arguments: providerParam);
    }

    private static Expression MakeKeyedImplementationFactoryExpression(object? serviceKey, ParameterExpression providerParam, KeyedServiceFactory keyedImplementationFactory)
    {
        // keyedImplementationFactory(provider, serviceKey)
        return Expression.Invoke(
            expression: Expression.Constant(keyedImplementationFactory),
            arguments: [providerParam, Expression.Constant(serviceKey)]);
    }

    public static ServiceContainerBuilder Create(IServiceCollection services)
    {
        return new ServiceContainerBuilder(services, new ServiceRegisterBuilder());
    }
}
