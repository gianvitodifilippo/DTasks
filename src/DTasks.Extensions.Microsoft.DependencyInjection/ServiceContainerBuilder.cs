using DTasks.Extensions.Microsoft.DependencyInjection.Hosting;
using DTasks.Extensions.Microsoft.DependencyInjection.Mapping;
using DTasks.Extensions.Microsoft.DependencyInjection.Utils;
using DTasks.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace DTasks.Extensions.Microsoft.DependencyInjection;

using ExpressionOrError = (bool IsError, Expression Expression);
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

    private readonly List<Exception> _validationErrors = [];

    public void AddDTaskServices()
    {
        IServiceRegister register = registerBuilder.Build();

        DAsyncServiceValidator validator = _validationErrors.Count == 0
            ? () => { }
            : () => throw new AggregateException("Some d-async services are not able to be constructed.", _validationErrors);

        services
            .AddSingleton(register)
            .AddSingleton(validator)
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

        ParameterExpression providerParam = Expression.Parameter(typeof(IServiceProvider), "provider");
        Expression body = MakeServiceFactoryBody(descriptor, providerParam, token);

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

    private Expression MakeServiceFactoryBody(ServiceDescriptor descriptor, ParameterExpression providerParam, ServiceToken token)
    {
        ExpressionOrError mappedInstanceResult = MakeMappedInstanceExpression(descriptor, providerParam);
        if (mappedInstanceResult.IsError)
            return mappedInstanceResult.Expression;

        MethodInfo mapMethod = descriptor.Lifetime switch
        {
            ServiceLifetime.Singleton => _mapSingletonMethod,
            ServiceLifetime.Scoped => _mapScopedMethod,
            ServiceLifetime.Transient => _mapTransientMethod,
            _ => throw new InvalidOperationException($"Invalid service lifetime: '{descriptor.Lifetime}'.")
        };

        // provider.GetRequiredService<IServiceMapper>().`mapMethod`(provider, `mappedInstanceExpr`, token)
        return Expression.Call(
            instance: Expression.Call(
                method: _getServiceMapperMethod,
                arg0: providerParam),
            method: mapMethod,
            arg0: providerParam,
            arg1: mappedInstanceResult.Expression,
            arg2: Expression.Constant(token));
    }

    private ExpressionOrError MakeMappedInstanceExpression(ServiceDescriptor descriptor, ParameterExpression providerParam)
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

    private ExpressionOrError MakeImplementationTypeExpression(ServiceDescriptor descriptor, ParameterExpression providerParam, Type implementationType)
    {
        ConstructorInfo? constructor = _constructorLocator.GetDependencyInjectionConstructor(implementationType);
        if (constructor is null)
            return ExpectError(descriptor, implementationType);

        ParameterInfo[] parameters = constructor.GetParameters();
        Expression[] arguments = new Expression[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            ParameterInfo parameter = parameters[i];

            ExpressionOrError implementationArgumentResult = MakeImplementationArgument(descriptor, providerParam, implementationType, parameter);
            if (implementationArgumentResult.IsError)
                return implementationArgumentResult;

            arguments[i] = implementationArgumentResult.Expression;
        }

        // new `implementationType`(`arguments`)
        return Ok(Expression.New(constructor, arguments));
    }

    private ExpressionOrError MakeImplementationArgument(ServiceDescriptor descriptor, ParameterExpression providerParam, Type implementationType, ParameterInfo parameter)
    {
        bool isDAsyncDependency = parameter.IsDefined(typeof(DAsyncServiceAttribute), inherit: true);
        bool hasDefaultValue = ParameterDefaultValue.TryGetDefaultValue(parameter, out object? defaultValue);

        if (parameter.IsDefined(typeof(ServiceKeyAttribute), inherit: true))
            return MakeServiceKeyArgument(descriptor, implementationType, parameter, isDAsyncDependency);

        if (parameter.GetCustomAttribute<FromKeyedServicesAttribute>(inherit: true) is { } fromKeyedServiceAttribute)
        {
            MethodInfo getDependencyGenericMethod = (isDAsyncDependency, hasDefaultValue) switch
            {
                (true, true) => _getKeyedDAsyncServiceGenericMethod,
                (true, false) => _getRequiredKeyedDAsyncServiceGenericMethod,
                (false, true) => _getKeyedServiceGenericMethod,
                (false, false) => _getRequiredKeyedServiceGenericMethod
            };

            Expression getDependency = Expression.Call(
                method: getDependencyGenericMethod.MakeGenericMethod(parameter.ParameterType),
                arg0: providerParam,
                arg1: Expression.Constant(fromKeyedServiceAttribute.Key));

            return Ok(hasDefaultValue
                ? Coalesce(getDependency, parameter, defaultValue)
                : getDependency);
        }
        else
        {
            MethodInfo getDependencyGenericMethod = (isDAsyncDependency, hasDefaultValue) switch
            {
                (true, true) => _getDAsyncServiceGenericMethod,
                (true, false) => _getRequiredDAsyncServiceGenericMethod,
                (false, true) => _getServiceGenericMethod,
                (false, false) => _getRequiredServiceGenericMethod
            };

            Expression getDependency = Expression.Call(
                method: getDependencyGenericMethod.MakeGenericMethod(parameter.ParameterType),
                arg0: providerParam);

            return Ok(hasDefaultValue
                ? Coalesce(getDependency, parameter, defaultValue)
                : getDependency);
        }

        static Expression Coalesce(Expression getDependency, ParameterInfo parameter, object? defaultValue) => Expression.Coalesce(
            left: getDependency,
            right: Expression.Constant(defaultValue, parameter.ParameterType));
    }

    private ExpressionOrError MakeServiceKeyArgument(ServiceDescriptor descriptor, Type implementationType, ParameterInfo parameter, bool isDAsyncDependency)
    {
        if (isDAsyncDependency)
        {
            var exception = new InvalidOperationException($"""
                A constructor parameter may not be decorated with both [ServiceKey] and [DAsyncService].
                The implementation type that contains this parameter is '{implementationType.Name}'.
                """);

            _validationErrors.Add(exception);
            return Error(Expression.Throw(Expression.Constant(exception)));
        }

        if (!descriptor.IsKeyedService || descriptor.ServiceKey is not object serviceKey || serviceKey.GetType() != parameter.ParameterType)
            return ExpectError(descriptor, implementationType);

        return Ok(Expression.Constant(serviceKey, parameter.ParameterType));
    }

    private ExpressionOrError ExpectError(ServiceDescriptor descriptor, Type implementationType)
    {
        // NotSupportedException because it should only be thrown if the service provider doesn't throw when validating the service.
        // This means that we don't support something that passes validation.

        var exception = new NotSupportedException($"Unable to activate type '{implementationType.Name}' as a d-async service.");
        _validationErrors.Add(exception);

        // Let's inject the service using a key that's accessible only here so that we can delegate the job to the service provider when we expect a validation error
        object helperKey = new();
        services.Add(new ServiceDescriptor(descriptor.ServiceType, helperKey, implementationType, descriptor.Lifetime));

        return Error(Expression.Block(
            arg0: Expression.Call(
                method: _getRequiredKeyedServiceGenericMethod.MakeGenericMethod(descriptor.ServiceType),
                arg0: Expression.Constant(helperKey)),
            arg1: Expression.Throw(Expression.Constant(exception))));
    }

    private static ExpressionOrError MakeImplementationInstanceExpression(object implementationInstance)
    {
        // implementationInstance
        return Ok(Expression.Constant(implementationInstance));
    }

    private static ExpressionOrError MakeImplementationFactoryExpression(ParameterExpression providerParam, ServiceFactory implementationFactory)
    {
        // implementationFactory(provider)
        return Ok(Expression.Invoke(
            expression: Expression.Constant(implementationFactory),
            arguments: providerParam));
    }

    private static ExpressionOrError MakeKeyedImplementationFactoryExpression(object? serviceKey, ParameterExpression providerParam, KeyedServiceFactory keyedImplementationFactory)
    {
        // keyedImplementationFactory(provider, serviceKey)
        return Ok(Expression.Invoke(
            expression: Expression.Constant(keyedImplementationFactory),
            arguments: [providerParam, Expression.Constant(serviceKey)]));
    }

    private static ExpressionOrError Ok(Expression expression) => (false, expression);

    private static ExpressionOrError Error(Expression expression) => (true, expression);

    public static ServiceContainerBuilder Create(IServiceCollection services)
    {
        return new ServiceContainerBuilder(services, new ServiceRegisterBuilder());
    }
}
