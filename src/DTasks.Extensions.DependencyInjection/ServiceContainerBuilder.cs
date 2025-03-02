using DTasks.Extensions.DependencyInjection.Mapping;
using DTasks.Extensions.DependencyInjection.Marshaling;
using DTasks.Extensions.DependencyInjection.Utils;
using DTasks.Marshaling;
using DTasks.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace DTasks.Extensions.DependencyInjection;

using ExpressionOrError = (bool IsError, Expression Expression);
using KeyedServiceFactory = Func<IServiceProvider, object?, object>;
using ServiceFactory = Func<IServiceProvider, object>;

internal sealed class ServiceContainerBuilder(
    IServiceCollection services,
    ITypeResolverBuilder typeResolverBuilder,
    IDAsyncServiceRegisterBuilder registerBuilder) : IServiceContainerBuilder
{
    private readonly MethodInfo _mapSingletonMethod = typeof(IServiceMapper).GetRequiredMethod(
        name: nameof(IServiceMapper.MapSingleton),
        genericParameterCount: 0,
        bindingAttr: BindingFlags.Instance | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider), typeof(object), typeof(ServiceToken)]);

    private readonly MethodInfo _mapScopedMethod = typeof(IServiceMapper).GetRequiredMethod(
        name: nameof(IServiceMapper.MapScoped),
        genericParameterCount: 0,
        bindingAttr: BindingFlags.Instance | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider), typeof(object), typeof(ServiceToken)]);

    private readonly MethodInfo _mapTransientMethod = typeof(IServiceMapper).GetRequiredMethod(
        name: nameof(IServiceMapper.MapTransient),
        genericParameterCount: 0,
        bindingAttr: BindingFlags.Instance | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider), typeof(object), typeof(ServiceToken)]);

    private readonly MethodInfo _getServiceMapperMethod = typeof(ServiceProviderServiceExtensions).GetRequiredMethod(
        name: nameof(ServiceProviderServiceExtensions.GetRequiredService),
        genericParameterCount: 1,
        bindingAttr: BindingFlags.Static | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider)])
        .MakeGenericMethod(typeof(IServiceMapper));

    private readonly MethodInfo _getServiceGenericMethod = typeof(ServiceProviderServiceExtensions).GetRequiredMethod(
        name: nameof(ServiceProviderServiceExtensions.GetService),
        genericParameterCount: 1,
        bindingAttr: BindingFlags.Static | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider)]);

    private readonly MethodInfo _getKeyedServiceGenericMethod = typeof(ServiceProviderKeyedServiceExtensions).GetRequiredMethod(
        name: nameof(ServiceProviderKeyedServiceExtensions.GetKeyedService),
        genericParameterCount: 1,
        bindingAttr: BindingFlags.Static | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider), typeof(object)]);

    private readonly MethodInfo _getDAsyncServiceGenericMethod = typeof(DTasksServiceProviderExtensions).GetRequiredMethod(
        name: nameof(DTasksServiceProviderExtensions.GetDAsyncService),
        genericParameterCount: 1,
        bindingAttr: BindingFlags.Static | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider)]);

    private readonly MethodInfo _getKeyedDAsyncServiceGenericMethod = typeof(DTasksServiceProviderExtensions).GetRequiredMethod(
        name: nameof(DTasksServiceProviderExtensions.GetKeyedDAsyncService),
        genericParameterCount: 1,
        bindingAttr: BindingFlags.Static | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider), typeof(object)]);

    private readonly MethodInfo _getRequiredServiceGenericMethod = typeof(ServiceProviderServiceExtensions).GetRequiredMethod(
        name: nameof(ServiceProviderServiceExtensions.GetRequiredService),
        genericParameterCount: 1,
        bindingAttr: BindingFlags.Static | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider)]);

    private readonly MethodInfo _getRequiredKeyedServiceGenericMethod = typeof(ServiceProviderKeyedServiceExtensions).GetRequiredMethod(
        name: nameof(ServiceProviderKeyedServiceExtensions.GetRequiredKeyedService),
        genericParameterCount: 1,
        bindingAttr: BindingFlags.Static | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider), typeof(object)]);

    private readonly MethodInfo _getRequiredDAsyncServiceGenericMethod = typeof(DTasksServiceProviderExtensions).GetRequiredMethod(
        name: nameof(DTasksServiceProviderExtensions.GetRequiredDAsyncService),
        genericParameterCount: 1,
        bindingAttr: BindingFlags.Static | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider)]);

    private readonly MethodInfo _getRequiredKeyedDAsyncServiceGenericMethod = typeof(DTasksServiceProviderExtensions).GetRequiredMethod(
        name: nameof(DTasksServiceProviderExtensions.GetRequiredKeyedDAsyncService),
        genericParameterCount: 1,
        bindingAttr: BindingFlags.Static | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider), typeof(object)]);

    private readonly MethodInfo _getRequiredKeyedServiceMethod = typeof(ServiceProviderKeyedServiceExtensions).GetRequiredMethod(
        name: nameof(ServiceProviderKeyedServiceExtensions.GetRequiredKeyedService),
        genericParameterCount: 0,
        bindingAttr: BindingFlags.Static | BindingFlags.Public,
        parameterTypes: [typeof(IServiceProvider), typeof(Type), typeof(object)]);

    private readonly ConstructorLocator _constructorLocator = new(services);

    private readonly List<Exception> _validationErrors = [];

    public void AddDTaskServices()
    {
        ITypeResolver typeResolver = typeResolverBuilder.Build();
        IDAsyncServiceRegister register = registerBuilder.Build(typeResolver);

        DAsyncServiceValidator validator = _validationErrors.Count == 0
            ? () => { }
            : () => throw new AggregateException("Some d-async services are not able to be constructed.", _validationErrors);

        services
            .AddSingleton(typeResolver)
            .AddSingleton(register)
            .AddSingleton(validator)
            .AddSingleton<IServiceMapper, ServiceMapper>()
            .AddSingleton<RootServiceProviderDAsyncMarshaler>()
            .AddSingleton<IRootDAsyncMarshaler>(provider => provider.GetRequiredService<RootServiceProviderDAsyncMarshaler>())
            .AddSingleton<IRootServiceMapper>(provider => provider.GetRequiredService<RootServiceProviderDAsyncMarshaler>())
            .AddScoped<ChildServiceProviderDAsyncMarshaler>()
            .AddScoped<IDAsyncMarshaler>(provider => provider.GetRequiredService<ChildServiceProviderDAsyncMarshaler>())
            .AddScoped<IChildServiceMapper>(provider => provider.GetRequiredService<ChildServiceProviderDAsyncMarshaler>());
    }

    public void Replace(ServiceDescriptor descriptor)
    {
        Debug.Assert(!descriptor.ServiceType.ContainsGenericParameters, "Open generic services can't be replaced.");

        TypeId typeId = registerBuilder.AddServiceType(descriptor.ServiceType);
        ServiceToken token = descriptor.IsKeyedService
            ? ServiceToken.Create(typeId, descriptor.ServiceKey!)
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

    private Expression MakeServiceFactoryBody(ServiceDescriptor descriptor, Expression providerExpr, ServiceToken token)
    {
        ExpressionOrError mappedInstanceResult = MakeMappedInstanceExpression(descriptor, providerExpr);
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
                arg0: providerExpr),
            method: mapMethod,
            arg0: providerExpr,
            arg1: mappedInstanceResult.Expression,
            arg2: Expression.Constant(token));
    }

    private ExpressionOrError MakeMappedInstanceExpression(ServiceDescriptor descriptor, Expression providerExpr)
    {
        if (descriptor.IsKeyedService)
        {
            if (descriptor.KeyedImplementationType is Type keyedImplementationType)
                return MakeImplementationTypeExpression(descriptor, providerExpr, keyedImplementationType);

            if (descriptor.KeyedImplementationInstance is object keyedImplementationInstance)
                return MakeImplementationInstanceExpression(keyedImplementationInstance);

            if (descriptor.KeyedImplementationFactory is KeyedServiceFactory keyedImplementationFactory)
                return MakeKeyedImplementationFactoryExpression(descriptor.ServiceKey, providerExpr, keyedImplementationFactory);
        }
        else
        {
            if (descriptor.ImplementationType is Type implementationType)
                return MakeImplementationTypeExpression(descriptor, providerExpr, implementationType);

            if (descriptor.ImplementationInstance is object implementationInstance)
                return MakeImplementationInstanceExpression(implementationInstance);

            if (descriptor.ImplementationFactory is ServiceFactory implementationFactory)
                return MakeImplementationFactoryExpression(providerExpr, implementationFactory);
        }

        throw new InvalidOperationException("Invalid service descriptor.");
    }

    private ExpressionOrError MakeImplementationTypeExpression(ServiceDescriptor descriptor, Expression providerExpr, Type implementationType)
    {
        ConstructorInfo? constructor = _constructorLocator.GetDependencyInjectionConstructor(descriptor, implementationType);
        if (constructor is null)
            return ExpectError(descriptor, providerExpr, implementationType);

        ParameterInfo[] parameters = constructor.GetParameters();
        Expression[] argumentExprs = new Expression[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            ParameterInfo parameter = parameters[i];

            ExpressionOrError implementationArgumentResult = MakeImplementationArgument(descriptor, providerExpr, implementationType, parameter);
            if (implementationArgumentResult.IsError)
                return implementationArgumentResult;

            argumentExprs[i] = implementationArgumentResult.Expression;
        }

        // new `implementationType`(`arguments`)
        return Ok(Expression.New(constructor, argumentExprs));
    }

    private ExpressionOrError MakeImplementationArgument(ServiceDescriptor descriptor, Expression providerExpr, Type implementationType, ParameterInfo parameter)
    {
        bool isDAsyncDependency = parameter.IsDefined(typeof(DAsyncServiceAttribute), inherit: true);
        bool hasDefaultValue = ParameterDefaultValue.TryGetDefaultValue(parameter, out object? defaultValue);
        bool isServiceKey = parameter.IsDefined(typeof(ServiceKeyAttribute), inherit: true);

        if (isServiceKey && descriptor.ServiceKey is object serviceKey)
            return MakeServiceKeyArgument(descriptor, providerExpr, implementationType, parameter, serviceKey, isDAsyncDependency);

        if (parameter.GetCustomAttribute<FromKeyedServicesAttribute>(inherit: true) is { } fromKeyedServiceAttribute)
        {
            MethodInfo getDependencyGenericMethod = (isDAsyncDependency, hasDefaultValue) switch
            {
                (true, true) => _getKeyedDAsyncServiceGenericMethod,
                (true, false) => _getRequiredKeyedDAsyncServiceGenericMethod,
                (false, true) => _getKeyedServiceGenericMethod,
                (false, false) => _getRequiredKeyedServiceGenericMethod
            };

            // provider.`getDependencyGenericMethod`<`parameter.ParameterType`>(`fromKeyedServiceAttribute.Key`)
            Expression getDependency = Expression.Call(
                method: getDependencyGenericMethod.MakeGenericMethod(parameter.ParameterType),
                arg0: providerExpr,
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

            // provider.`getDependencyGenericMethod`<`parameter.ParameterType`>()
            Expression getDependency = Expression.Call(
                method: getDependencyGenericMethod.MakeGenericMethod(parameter.ParameterType),
                arg0: providerExpr);

            return Ok(hasDefaultValue
                ? Coalesce(getDependency, parameter, defaultValue)
                : getDependency);
        }

        // getDependency ?? `defaultValue`
        static Expression Coalesce(Expression getDependency, ParameterInfo parameter, object? defaultValue) => Expression.Coalesce(
            left: getDependency,
            right: Expression.Constant(defaultValue, parameter.ParameterType));
    }

    private ExpressionOrError MakeServiceKeyArgument(ServiceDescriptor descriptor, Expression providerExpr, Type implementationType, ParameterInfo parameter, object serviceKey, bool isDAsyncDependency)
    {
        if (isDAsyncDependency)
        {
            var exception = new InvalidOperationException($"""
                A constructor parameter may not be decorated with both [ServiceKey] and [DAsyncService].
                The implementation type that contains this parameter is '{implementationType.Name}'.
                """);

            _validationErrors.Add(exception);

            // throw `exception`
            return Error(Expression.Throw(Expression.Constant(exception), descriptor.ServiceType));
        }

        if (serviceKey.GetType() != parameter.ParameterType)
            return ExpectError(descriptor, providerExpr, implementationType);

        // `serviceKey`
        return Ok(Expression.Constant(serviceKey, parameter.ParameterType));
    }

    private ExpressionOrError ExpectError(ServiceDescriptor descriptor, Expression providerExpr, Type implementationType)
    {
        // Instead of mimicking the service provider and throwing the errors it is supposed to produce, we inject the service
        // using a key that's accessible only here so that we can delegate the job to the service provider when we expect a validation error.
        // The expected validation errors that are thrown explicitly while constructing the call sites are:
        // 1. NoConstructorMatch: A suitable constructor for type 'implementationType' could not be located.
        // 2. UnableToActivateType: No constructor for type 'implementationType' can be instantiated using services from the service container and default values.
        // 3. InvalidServiceKeyType: The type of the key used for lookup doesn't match the type in the constructor parameter with the [ServiceKey] attribute.
        // Plus, we expect an error when we detect that a service has unresolvable dependencies.
        // If this approach proves to be too brittle, we will be forced to change this implementation and throw those errors instead of the service provider.

        // If the service provider doesn't throw when validating the service, we throw a NotSupportedException.
        // This means that our validation is not in sync with the underlying one.
        var exception = new NotSupportedException($"Unable to activate type '{implementationType.Name}' as a d-async service.");
        _validationErrors.Add(exception);

        // Here, we use a combination of dirty tricks to make the service provider throw the exceptions it's supposed to hadn't the service descriptor being replaced:
        // 1. The key type is a private type so that we ensure there is no chance of it being injected using the [ServiceKey] attribute.
        //    This way, when the expected error is InvalidServiceKeyType, the service provider throws because of the type being HelperKey,
        //    not the user's, but the difference can't be told by looking at the exception message.
        // 2. When the service is not keyed, but the user still used [ServiceKey] to decorate a constructor parameter, we artificially remove those attributes
        //    by using a NoServiceKeyTypeDelegator to avoid causing ourselves a InvalidServiceKeyType where it shouldn't have been.
        Type serviceType = descriptor.ServiceType;
        var helperKey = new HelperKey();
        Type helperImplementationType = descriptor.IsKeyedService ? implementationType : new NoServiceKeyTypeDelegator(implementationType);

        services.Add(new ServiceDescriptor(serviceType, helperKey, helperImplementationType, descriptor.Lifetime));

        // {
        //     _ = provider.GetRequiredKeyedService(`serviceType`, `helperKey`);
        //     throw `exception`;
        // }
        return Error(Expression.Block([
            Expression.Call(
                method: _getRequiredKeyedServiceMethod,
                arg0: providerExpr,
                arg1: Expression.Constant(serviceType),
                arg2: Expression.Constant(helperKey)),
            Expression.Throw(Expression.Constant(exception), serviceType)
        ]));
    }

    private static ExpressionOrError MakeImplementationInstanceExpression(object implementationInstance)
    {
        // `implementationInstance`
        return Ok(Expression.Constant(implementationInstance));
    }

    private static ExpressionOrError MakeImplementationFactoryExpression(Expression providerExpr, ServiceFactory implementationFactory)
    {
        // `implementationFactory`(provider)
        return Ok(Expression.Invoke(
            expression: Expression.Constant(implementationFactory),
            arguments: providerExpr));
    }

    private static ExpressionOrError MakeKeyedImplementationFactoryExpression(object? serviceKey, Expression providerExpr, KeyedServiceFactory keyedImplementationFactory)
    {
        // `keyedImplementationFactory`(provider, `serviceKey`)
        return Ok(Expression.Invoke(
            expression: Expression.Constant(keyedImplementationFactory),
            arguments: [providerExpr, Expression.Constant(serviceKey)]));
    }

    private static ExpressionOrError Ok(Expression expression) => (false, expression);

    private static ExpressionOrError Error(Expression expression) => (true, expression);

    private sealed class HelperKey;

    private sealed class NoServiceKeyTypeDelegator(Type serviceType) : TypeDelegator(serviceType)
    {
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            return base.GetConstructors(bindingAttr)
                .Select(constructor => new NoServiceKeyConstructorDelegator(constructor))
                .ToArray();
        }

        public override string ToString() => typeImpl.ToString();
    }

    private sealed class NoServiceKeyConstructorDelegator(ConstructorInfo constructor) : ConstructorDelegator(constructor)
    {
        public override ParameterInfo[] GetParameters()
        {
            return _constructor.GetParameters()
                .Select(parameter => new NoServiceKeyParameterDelegator(parameter))
                .ToArray();
        }
    }

    private sealed class NoServiceKeyParameterDelegator(ParameterInfo parameter) : ParameterDelegator(parameter)
    {
        public override IEnumerable<CustomAttributeData> CustomAttributes => _parameter.CustomAttributes.Where(IsNotServiceKeyAttribute);

        public override object[] GetCustomAttributes(bool inherit)
        {
            return _parameter.GetCustomAttributes(inherit)
                .Where(IsNotServiceKeyAttribute)
                .ToArray();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return _parameter.GetCustomAttributes(attributeType, inherit)
                .Where(IsNotServiceKeyAttribute)
                .ToArray();
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return _parameter.GetCustomAttributesData()
                .Where(IsNotServiceKeyAttribute)
                .ToList();
        }

        private static bool IsNotServiceKeyAttribute(object attribute) => attribute is not ServiceKeyAttribute;

        private static bool IsNotServiceKeyAttribute(CustomAttributeData data) => data.AttributeType != typeof(ServiceKeyAttribute);
    }
}
