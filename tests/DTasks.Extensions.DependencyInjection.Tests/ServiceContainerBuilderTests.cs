using DTasks.Extensions.DependencyInjection.Mapping;
using DTasks.Extensions.DependencyInjection.Marshaling;
using DTasks.Marshaling;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;
using static FluentAssertions.FluentActions;

namespace DTasks.Extensions.DependencyInjection;

using ServiceAccessor = Func<IKeyedServiceProvider, object?>;

public partial class ServiceContainerBuilderTests
{
    private readonly IServiceMapper _mapper;
    private readonly IDAsyncServiceRegisterBuilder _registerBuilder;
    private readonly IDAsyncServiceRegister _register;
    private readonly IServiceCollection _services;
    private readonly ITypeResolverBuilder typeResolverBuilder;
    private readonly TypeId _typeId;
    private readonly string _serviceKey;
    private readonly ServiceContainerBuilder _sut;

    public ServiceContainerBuilderTests()
    {
        _mapper = Substitute.For<IServiceMapper>();
        _registerBuilder = Substitute.For<IDAsyncServiceRegisterBuilder>();
        _register = Substitute.For<IDAsyncServiceRegister>();
        _services = new TestServiceCollection(_mapper);
        typeResolverBuilder = Substitute.For<ITypeResolverBuilder>();
        _typeId = new TypeId("typeId");
        _serviceKey = "serviceKey";
        _sut = new ServiceContainerBuilder(_services, typeResolverBuilder, _registerBuilder);

        _registerBuilder
            .AddServiceType(Arg.Any<Type>())
            .Returns(_typeId);

        _registerBuilder
            .Build(Arg.Any<ITypeResolver>())
            .Returns(_register);

        _mapper
            .MapSingleton(Arg.Any<IServiceProvider>(), Arg.Any<object>(), Arg.Any<ServiceToken>())
            .Returns(call => call[1]);

        _mapper
            .MapScoped(Arg.Any<IServiceProvider>(), Arg.Any<object>(), Arg.Any<ServiceToken>())
            .Returns(call => call[1]);

        _mapper
            .MapTransient(Arg.Any<IServiceProvider>(), Arg.Any<object>(), Arg.Any<ServiceToken>())
            .Returns(call => call[1]);
    }

    [Theory]
    [ImplementationTypeData]
    public void Replace_ReplacesSingletonThatHasImplementationType(Type serviceType, Type implementationType)
    {
        // Arrange
        ServiceDescriptor descriptor = ServiceDescriptor.Singleton(serviceType, implementationType);
        _services.Add(descriptor);

        // Act
        _sut.Replace(descriptor);
        _sut.AddDTaskServices();
        using var provider = BuildScopedServiceProvider(_services);

        // Assert
        _services.Should().NotContain(descriptor);

        object? service = provider.GetService(serviceType);
        service.Should().BeOfType(implementationType);
        AssertValidatorDoesNotThrow(provider);
        _mapper.Received(1).MapSingleton(Arg.Any<IServiceProvider>(), service!, Arg.Is(ServiceToken(_typeId)));
    }

    [Theory]
    [ImplementationInstanceData]
    public void Replace_ReplacesSingletonThatHasImplementationInstance(Type serviceType, object implementationInstance)
    {
        // Arrange
        ServiceDescriptor descriptor = ServiceDescriptor.Singleton(serviceType, implementationInstance);
        _services.Add(descriptor);

        // Act
        _sut.Replace(descriptor);
        _sut.AddDTaskServices();
        using var provider = BuildScopedServiceProvider(_services);

        // Assert
        _services.Should().NotContain(descriptor);

        object? service = provider.GetService(serviceType);
        service.Should().BeSameAs(implementationInstance);
        AssertValidatorDoesNotThrow(provider);
        _mapper.Received(1).MapSingleton(Arg.Any<IServiceProvider>(), service!, Arg.Is(ServiceToken(_typeId)));
    }

    [Theory]
    [ImplementationInstanceData]
    public void Replace_ReplacesSingletonThatHasImplementationFactory(Type serviceType, object implementationInstance)
    {
        // Arrange
        ServiceDescriptor descriptor = ServiceDescriptor.Singleton(serviceType, sp => implementationInstance);
        _services.Add(descriptor);

        // Act
        _sut.Replace(descriptor);
        _sut.AddDTaskServices();
        using var provider = BuildScopedServiceProvider(_services);

        // Assert
        _services.Should().NotContain(descriptor);

        object? service = provider.GetService(serviceType);
        service.Should().BeSameAs(implementationInstance);
        AssertValidatorDoesNotThrow(provider);
        _mapper.Received(1).MapSingleton(Arg.Any<IServiceProvider>(), service!, Arg.Is(ServiceToken(_typeId)));
    }

    [Theory]
    [ImplementationTypeData]
    public void Replace_ReplacesScopedThatHasImplementationType(Type serviceType, Type implementationType)
    {
        // Arrange
        ServiceDescriptor descriptor = ServiceDescriptor.Scoped(serviceType, implementationType);
        _services.Add(descriptor);

        // Act
        _sut.Replace(descriptor);
        _sut.AddDTaskServices();
        using var provider = BuildScopedServiceProvider(_services);

        // Assert
        _services.Should().NotContain(descriptor);

        object? service = provider.GetService(serviceType);
        service.Should().BeOfType(implementationType);
        AssertValidatorDoesNotThrow(provider);
        _mapper.Received(1).MapScoped(Arg.Any<IServiceProvider>(), service!, Arg.Is(ServiceToken(_typeId)));
    }

    [Theory]
    [ImplementationInstanceData]
    public void Replace_ReplacesScopedThatHasImplementationFactory(Type serviceType, object implementationInstance)
    {
        // Arrange
        ServiceDescriptor descriptor = ServiceDescriptor.Scoped(serviceType, sp => implementationInstance);
        _services.Add(descriptor);

        // Act
        _sut.Replace(descriptor);
        _sut.AddDTaskServices();
        using var provider = BuildScopedServiceProvider(_services);

        // Assert
        _services.Should().NotContain(descriptor);

        object? service = provider.GetService(serviceType);
        service.Should().BeSameAs(implementationInstance);
        AssertValidatorDoesNotThrow(provider);
        _mapper.Received(1).MapScoped(Arg.Any<IServiceProvider>(), service!, Arg.Is(ServiceToken(_typeId)));
    }

    [Theory]
    [ImplementationTypeData]
    public void Replace_ReplacesTransientThatHasImplementationType(Type serviceType, Type implementationType)
    {
        // Arrange
        ServiceDescriptor descriptor = ServiceDescriptor.Transient(serviceType, implementationType);
        _services.Add(descriptor);

        // Act
        _sut.Replace(descriptor);
        _sut.AddDTaskServices();
        using var provider = BuildScopedServiceProvider(_services);

        // Assert
        _services.Should().NotContain(descriptor);

        object? service = provider.GetService(serviceType);
        service.Should().BeOfType(implementationType);
        AssertValidatorDoesNotThrow(provider);
        _mapper.Received(1).MapTransient(Arg.Any<IServiceProvider>(), service!, Arg.Is(ServiceToken(_typeId)));
    }

    [Theory]
    [ImplementationInstanceData]
    public void Replace_ReplacesTransientThatHasImplementationFactory(Type serviceType, object implementationInstance)
    {
        // Arrange
        ServiceDescriptor descriptor = ServiceDescriptor.Transient(serviceType, sp => implementationInstance);
        _services.Add(descriptor);

        // Act
        _sut.Replace(descriptor);
        _sut.AddDTaskServices();
        using var provider = BuildScopedServiceProvider(_services);

        // Assert
        _services.Should().NotContain(descriptor);

        object? service = provider.GetService(serviceType);
        service.Should().BeSameAs(implementationInstance);
        AssertValidatorDoesNotThrow(provider);
        _mapper.Received(1).MapTransient(Arg.Any<IServiceProvider>(), service!, Arg.Is(ServiceToken(_typeId)));
    }

    [Theory]
    [ImplementationTypeData]
    public void Replace_ReplacesSingletonThatHasKeyedImplementationType(Type serviceType, Type implementationType)
    {
        // Arrange
        ServiceDescriptor descriptor = ServiceDescriptor.KeyedSingleton(serviceType, _serviceKey, implementationType);
        _services.Add(descriptor);

        // Act
        _sut.Replace(descriptor);
        _sut.AddDTaskServices();
        using var provider = BuildScopedServiceProvider(_services);

        // Assert
        _services.Should().NotContain(descriptor);

        object? service = provider.GetKeyedService(serviceType, _serviceKey);
        service.Should().BeOfType(implementationType);
        AssertValidatorDoesNotThrow(provider);
        _mapper.Received(1).MapSingleton(Arg.Any<IServiceProvider>(), service!, Arg.Is(KeyedServiceToken(_typeId, _serviceKey)));
    }

    [Theory]
    [ImplementationInstanceData]
    public void Replace_ReplacesSingletonThatHasKeyedImplementationInstance(Type serviceType, object implementationInstance)
    {
        // Arrange
        ServiceDescriptor descriptor = ServiceDescriptor.KeyedSingleton(serviceType, _serviceKey, implementationInstance);
        _services.Add(descriptor);

        // Act
        _sut.Replace(descriptor);
        _sut.AddDTaskServices();
        using var provider = BuildScopedServiceProvider(_services);

        // Assert
        _services.Should().NotContain(descriptor);

        object? service = provider.GetKeyedService(serviceType, _serviceKey);
        service.Should().BeSameAs(implementationInstance);
        AssertValidatorDoesNotThrow(provider);
        _mapper.Received(1).MapSingleton(Arg.Any<IServiceProvider>(), service!, Arg.Is(KeyedServiceToken(_typeId, _serviceKey)));
    }

    [Theory]
    [ImplementationInstanceData]
    public void Replace_ReplacesSingletonThatHasKeyedImplementationFactory(Type serviceType, object implementationInstance)
    {
        // Arrange
        ServiceDescriptor descriptor = ServiceDescriptor.KeyedSingleton(serviceType, _serviceKey, (sp, key) => implementationInstance);
        _services.Add(descriptor);

        // Act
        _sut.Replace(descriptor);
        _sut.AddDTaskServices();
        using var provider = BuildScopedServiceProvider(_services);

        // Assert
        _services.Should().NotContain(descriptor);

        object? service = provider.GetKeyedService(serviceType, _serviceKey);
        service.Should().BeSameAs(implementationInstance);
        AssertValidatorDoesNotThrow(provider);
        _mapper.Received(1).MapSingleton(Arg.Any<IServiceProvider>(), service!, Arg.Is(KeyedServiceToken(_typeId, _serviceKey)));
    }

    [Theory]
    [ImplementationTypeData]
    public void Replace_ReplacesScopedThatHasKeyedImplementationType(Type serviceType, Type implementationType)
    {
        // Arrange
        ServiceDescriptor descriptor = ServiceDescriptor.KeyedScoped(serviceType, _serviceKey, implementationType);
        _services.Add(descriptor);

        // Act
        _sut.Replace(descriptor);
        _sut.AddDTaskServices();
        using var provider = BuildScopedServiceProvider(_services);

        // Assert
        _services.Should().NotContain(descriptor);

        object? service = provider.GetKeyedService(serviceType, _serviceKey);
        service.Should().BeOfType(implementationType);
        AssertValidatorDoesNotThrow(provider);
        _mapper.Received(1).MapScoped(Arg.Any<IServiceProvider>(), service!, Arg.Is(KeyedServiceToken(_typeId, _serviceKey)));
    }

    [Theory]
    [ImplementationInstanceData]
    public void Replace_ReplacesScopedThatHasKeyedImplementationFactory(Type serviceType, object implementationInstance)
    {
        // Arrange
        ServiceDescriptor descriptor = ServiceDescriptor.KeyedScoped(serviceType, _serviceKey, (sp, key) => implementationInstance);
        _services.Add(descriptor);

        // Act
        _sut.Replace(descriptor);
        _sut.AddDTaskServices();
        using var provider = BuildScopedServiceProvider(_services);

        // Assert
        _services.Should().NotContain(descriptor);

        object? service = provider.GetKeyedService(serviceType, _serviceKey);
        service.Should().BeSameAs(implementationInstance);
        AssertValidatorDoesNotThrow(provider);
        _mapper.Received(1).MapScoped(Arg.Any<IServiceProvider>(), service!, Arg.Is(KeyedServiceToken(_typeId, _serviceKey)));
    }

    [Theory]
    [ImplementationTypeData]
    public void Replace_ReplacesTransientThatHasKeyedImplementationType(Type serviceType, Type implementationType)
    {
        // Arrange
        ServiceDescriptor descriptor = ServiceDescriptor.KeyedTransient(serviceType, _serviceKey, implementationType);
        _services.Add(descriptor);

        // Act
        _sut.Replace(descriptor);
        _sut.AddDTaskServices();
        using var provider = BuildScopedServiceProvider(_services);

        // Assert
        _services.Should().NotContain(descriptor);

        object? service = provider.GetKeyedService(serviceType, _serviceKey);
        service.Should().BeOfType(implementationType);
        AssertValidatorDoesNotThrow(provider);
        _mapper.Received(1).MapTransient(Arg.Any<IServiceProvider>(), service!, Arg.Is(KeyedServiceToken(_typeId, _serviceKey)));
    }

    [Theory]
    [ImplementationInstanceData]
    public void Replace_ReplacesTransientThatHasKeyedImplementationFactory(Type serviceType, object implementationInstance)
    {
        // Arrange
        ServiceDescriptor descriptor = ServiceDescriptor.KeyedTransient(serviceType, _serviceKey, (sp, key) => implementationInstance);
        _services.Add(descriptor);

        // Act
        _sut.Replace(descriptor);
        _sut.AddDTaskServices();
        using var provider = BuildScopedServiceProvider(_services);

        // Assert
        _services.Should().NotContain(descriptor);

        object? service = provider.GetKeyedService(serviceType, _serviceKey);
        service.Should().BeSameAs(implementationInstance);
        AssertValidatorDoesNotThrow(provider);
        _mapper.Received(1).MapTransient(Arg.Any<IServiceProvider>(), service!, Arg.Is(KeyedServiceToken(_typeId, _serviceKey)));
    }

    [Fact]
    public void Replace_HandlesAllKindsOfDependencies()
    {
        // Arrange
        string key = "key";
        Type serviceType = typeof(ServiceWithAllKindsOfDependencies);
        Dependency1 dependency1 = new();
        Dependency2 dependency2 = new();
        Dependency3 dependency3 = new();
        Dependency4 dependency4 = new();

        ServiceDescriptor descriptor1 = ServiceDescriptor.Singleton(dependency1);
        ServiceDescriptor descriptor2 = ServiceDescriptor.KeyedSingleton(ServiceWithAllKindsOfDependencies.Dep2Key, dependency2);
        ServiceDescriptor descriptor3 = ServiceDescriptor.Singleton(dependency3);
        ServiceDescriptor descriptor4 = ServiceDescriptor.KeyedSingleton(ServiceWithAllKindsOfDependencies.Dep4Key, dependency4);
        ServiceDescriptor targetDescriptor = ServiceDescriptor.KeyedSingleton(serviceType, key, serviceType);

        _register
            .IsDAsyncService(Arg.Any<Type>())
            .Returns(call =>
            {
                Type serviceType = call.Arg<Type>();
                return serviceType == typeof(Dependency3) || serviceType == typeof(Dependency4);
            });

        _services.Add(descriptor1);
        _services.Add(descriptor2);
        _services.Add(descriptor3);
        _services.Add(descriptor4);
        _services.Add(targetDescriptor);

        // Act
        _sut.Replace(descriptor3);
        _sut.Replace(descriptor4);
        _sut.Replace(targetDescriptor);
        _sut.AddDTaskServices();
        using var provider = BuildScopedServiceProvider(_services);

        // Assert
        object? service = provider.GetKeyedService(serviceType, key);
        service.Should().BeEquivalentTo(new ServiceWithAllKindsOfDependencies(
            key,
            dependency1,
            dependency2,
            dependency3,
            dependency4,
            Dependency5: null,
            Dependency6: null,
            Dependency7: null,
            Dependency8: null));
        AssertValidatorDoesNotThrow(provider);
    }

    [Fact]
    public void Replace_HandlesAmbiguousConstructor()
    {
        // Arrange
        Type serviceType = typeof(UnresolvableService);
        ServiceDescriptor descriptor1 = ServiceDescriptor.Singleton<Dependency1, Dependency1>();
        ServiceDescriptor descriptor2 = ServiceDescriptor.Singleton<Dependency2, Dependency2>();
        ServiceDescriptor targetDescriptor = ServiceDescriptor.Singleton(serviceType, serviceType);

        _services.Add(descriptor1);
        _services.Add(descriptor2);
        _services.Add(targetDescriptor);

        ServiceAccessor getService = provider => provider.GetService(serviceType);
        using var providerBeforeReplace = BuildScopedServiceProvider(_services);
        var expectedException = ServiceProviderException(providerBeforeReplace, getService);

        // Act
        _sut.Replace(targetDescriptor);
        _sut.AddDTaskServices();
        using var provider = BuildScopedServiceProvider(_services);

        // Assert
        AssertAccessorThrows(provider, getService, expectedException);
        AssertValidatorThrows<NotSupportedException>(provider, serviceType);
    }

    [Fact]
    public void Replace_HandlesUnresolvableConstructor()
    {
        // Arrange
        Type serviceType = typeof(UnresolvableService);
        ServiceDescriptor targetDescriptor = ServiceDescriptor.Singleton(serviceType, serviceType);
        _services.Add(targetDescriptor);

        ServiceAccessor getService = provider => provider.GetService(serviceType);
        using var providerBeforeReplace = BuildScopedServiceProvider(_services);
        var expectedException = ServiceProviderException(providerBeforeReplace, getService);

        // Act
        _sut.Replace(targetDescriptor);
        _sut.AddDTaskServices();
        using var provider = BuildScopedServiceProvider(_services);

        // Assert
        AssertAccessorThrows(provider, getService, expectedException);
        AssertValidatorThrows<NotSupportedException>(provider, serviceType);
    }

    [Fact]
    public void Replace_HandlesMissingDependencies()
    {
        // Arrange
        Type serviceType = typeof(ServiceWithOneDependency);
        ServiceDescriptor targetDescriptor = ServiceDescriptor.Singleton(serviceType, serviceType);
        _services.Add(targetDescriptor);

        ServiceAccessor getService = provider => provider.GetService(serviceType);
        using var providerBeforeReplace = BuildScopedServiceProvider(_services);
        var expectedException = ServiceProviderException(providerBeforeReplace, getService);

        // Act
        _sut.Replace(targetDescriptor);
        _sut.AddDTaskServices();
        using var provider = BuildScopedServiceProvider(_services);

        // Assert
        AssertAccessorThrows(provider, getService, expectedException);
        AssertValidatorThrows<NotSupportedException>(provider, serviceType);
    }

    [Fact]
    public void Replace_HandlesPrivateConstructor()
    {
        // Arrange
        Type serviceType = typeof(ServiceWithPrivateConstructor);
        ServiceDescriptor targetDescriptor = ServiceDescriptor.Singleton(serviceType, serviceType);
        _services.Add(targetDescriptor);

        ServiceAccessor getService = provider => provider.GetService(serviceType);
        using var providerBeforeReplace = BuildScopedServiceProvider(_services);
        var expectedException = ServiceProviderException(providerBeforeReplace, getService);

        // Act
        _sut.Replace(targetDescriptor);
        _sut.AddDTaskServices();
        using var provider = BuildScopedServiceProvider(_services);

        // Assert
        AssertAccessorThrows(provider, getService, expectedException);
        AssertValidatorThrows<NotSupportedException>(provider, serviceType);
    }

    [Fact]
    public void Replace_HandlesServiceKey_WhenServiceIsNotKeyed()
    {
        // Arrange
        Type serviceType = typeof(ServiceWithStringServiceKey);
        ServiceDescriptor targetDescriptor = ServiceDescriptor.Singleton(serviceType, serviceType);
        _services.Add(targetDescriptor);

        ServiceAccessor getService = provider => provider.GetService(serviceType);
        using var providerBeforeReplace = BuildScopedServiceProvider(_services);
        var expectedException = ServiceProviderException(providerBeforeReplace, getService);

        // Act
        _sut.Replace(targetDescriptor);
        _sut.AddDTaskServices();
        using var provider = BuildScopedServiceProvider(_services);

        // Assert
        AssertAccessorThrows(provider, getService, expectedException);
        AssertValidatorThrows<NotSupportedException>(provider, serviceType);
    }

    [Fact]
    public void Replace_HandlesServiceKey_WhenServiceIsNotKeyedButAServiceWithThatTypeWasRegistered()
    {
        // Arrange
        Type serviceType = typeof(ServiceWithStringServiceKey);
        string keyAsService = "whatever";
        ServiceDescriptor targetDescriptor = ServiceDescriptor.Singleton(serviceType, serviceType);
        _services.Add(targetDescriptor);
        _services.AddSingleton(keyAsService);

        // Act
        _sut.Replace(targetDescriptor);
        _sut.AddDTaskServices();
        using var provider = BuildScopedServiceProvider(_services);

        // Assert
        object? service = provider.GetService(serviceType);
        service.Should().BeEquivalentTo(new ServiceWithStringServiceKey(keyAsService));
        AssertValidatorDoesNotThrow(provider);
    }

    [Fact]
    public void Replace_HandlesServiceKey_WhenServiceIsNotKeyedButParameterHasDefaultValue()
    {
        // Arrange
        Type serviceType = typeof(ServiceWithDefaultedServiceKey);
        ServiceDescriptor targetDescriptor = ServiceDescriptor.Singleton(serviceType, serviceType);
        _services.Add(targetDescriptor);

        // Act
        _sut.Replace(targetDescriptor);
        _sut.AddDTaskServices();
        using var provider = BuildScopedServiceProvider(_services);

        // Assert
        object? service = provider.GetService(serviceType);
        service.Should().BeEquivalentTo(new ServiceWithDefaultedServiceKey());
        AssertValidatorDoesNotThrow(provider);
    }

    [Fact]
    public void Replace_HandlesServiceKey_WhenServiceKeyParameterHasInvalidTypeAndItIsNotObject()
    {
        // Arrange
        Type serviceType = typeof(ServiceWithStringServiceKey);
        int serviceKey = 42;
        ServiceDescriptor targetDescriptor = ServiceDescriptor.KeyedSingleton(serviceType, serviceKey, serviceType);
        _services.Add(targetDescriptor);

        ServiceAccessor getService = provider => provider.GetKeyedService(serviceType, serviceKey);
        using var providerBeforeReplace = BuildScopedServiceProvider(_services);
        var expectedException = ServiceProviderException(providerBeforeReplace, getService);

        // Act
        _sut.Replace(targetDescriptor);
        _sut.AddDTaskServices();
        using var provider = BuildScopedServiceProvider(_services);

        // Assert
        AssertAccessorThrows(provider, getService, expectedException);
        AssertValidatorThrows<NotSupportedException>(provider, serviceType);
    }

    [Fact]
    public void Replace_HandlesServiceKey_WhenServiceKeyParameterHasInvalidTypeAndItIsObject()
    {
        // Arrange
        Type serviceType = typeof(ServiceWithObjectServiceKey);
        int serviceKey = 42;
        ServiceDescriptor targetDescriptor = ServiceDescriptor.KeyedSingleton(serviceType, serviceKey, serviceType);
        _services.Add(targetDescriptor);

        ServiceAccessor getService = provider => provider.GetKeyedService(serviceType, serviceKey);
        using var providerBeforeReplace = BuildScopedServiceProvider(_services);
        var expectedException = ServiceProviderException(providerBeforeReplace, getService);

        // Act
        _sut.Replace(targetDescriptor);
        _sut.AddDTaskServices();
        using var provider = BuildScopedServiceProvider(_services);

        // Assert
        AssertAccessorThrows(provider, getService, expectedException);
        AssertValidatorThrows<NotSupportedException>(provider, serviceType);
    }

    [Fact]
    public void Replace_Throws_WhenParameterIsDecoratedWithBothServiceKeyAttributeAndDAsyncServiceAttribute()
    {
        // Arrange
        Type serviceType = typeof(ServiceWithServiceKeyAsDAsyncService);
        int serviceKey = 42;
        ServiceDescriptor targetDescriptor = ServiceDescriptor.KeyedSingleton(serviceType, serviceKey, serviceType);
        _services.Add(targetDescriptor);

        // Act
        _sut.Replace(targetDescriptor);
        _sut.AddDTaskServices();
        using var provider = BuildScopedServiceProvider(_services);

        // Assert
        Invoking(() => provider.GetKeyedService(serviceType, serviceKey)).Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain(serviceType.Name);
        AssertValidatorThrows<InvalidOperationException>(provider, serviceType);
    }

    private static void AssertValidatorDoesNotThrow(IKeyedServiceProvider provider)
    {
        var validator = provider.GetRequiredService<DAsyncServiceValidator>();
        Invoking(() => validator()).Should().NotThrow();
    }

    private static void AssertValidatorThrows<TException>(IKeyedServiceProvider provider, Type serviceType)
        where TException : Exception
    {
        var validator = provider.GetRequiredService<DAsyncServiceValidator>();
        Invoking(() => validator()).Should().ThrowExactly<AggregateException>()
            .Which.InnerExceptions.Should().ContainSingle(ex => ex is TException && ex.Message.Contains(serviceType.Name));
    }

    private static void AssertAccessorThrows(IKeyedServiceProvider provider, ServiceAccessor getService, Expression<Func<Exception, bool>> expectedException)
    {
        provider.Invoking(getService).Should().Throw<Exception>().Which.Should().Match(expectedException);
    }
}
