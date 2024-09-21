using DTasks.Extensions.Microsoft.DependencyInjection.Hosting;
using DTasks.Extensions.Microsoft.DependencyInjection.Mapping;
using DTasks.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.Microsoft.DependencyInjection;

public partial class ServiceContainerBuilderTests
{
    private const string TypeId = "typeId";

    private readonly IServiceCollection _services;
    private readonly IServiceRegisterBuilder _registerBuilder;
    private readonly IServiceMapper _mapper;
    private readonly ServiceTypeId _typeId;
    private readonly object _serviceKey;
    private readonly ServiceContainerBuilder _sut;

    public ServiceContainerBuilderTests()
    {
        _services = new ServiceCollection();
        _registerBuilder = Substitute.For<IServiceRegisterBuilder>();
        _mapper = Substitute.For<IServiceMapper>();
        _typeId = new ServiceTypeId(TypeId);
        _serviceKey = new();
        _sut = new ServiceContainerBuilder(_services, _registerBuilder);

        _services.AddSingleton(_mapper);

        _registerBuilder
            .AddServiceType(Arg.Any<Type>())
            .Returns(_typeId);

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

    [Fact]
    public void AddDTaskServices_AddsConsumerServices()
    {
        // Arrange

        // Act
        _sut.AddDTaskServices();

        // Assert
        _services.Should()
            .ContainSingle(Singleton<IRootDTaskScope>()).And
            .ContainSingle(Scoped<IDTaskScope>());
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

        // Assert
        _services.Should().NotContain(descriptor);

        object service = GetService(serviceType);
        _mapper.Received(1).MapSingleton(Arg.Any<IServiceProvider>(), service, Arg.Is(ServiceToken(TypeId)));
        service.Should().BeOfType(implementationType);
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

        // Assert
        _services.Should().NotContain(descriptor);

        object service = GetService(serviceType);
        _mapper.Received(1).MapSingleton(Arg.Any<IServiceProvider>(), service, Arg.Is(ServiceToken(TypeId)));
        service.Should().BeSameAs(implementationInstance);
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

        // Assert
        _services.Should().NotContain(descriptor);

        object service = GetService(serviceType);
        _mapper.Received(1).MapSingleton(Arg.Any<IServiceProvider>(), service, Arg.Is(ServiceToken(TypeId)));
        service.Should().BeSameAs(implementationInstance);
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

        // Assert
        _services.Should().NotContain(descriptor);

        object service = GetService(serviceType);
        _mapper.Received(1).MapScoped(Arg.Any<IServiceProvider>(), service, Arg.Is(ServiceToken(TypeId)));
        service.Should().BeOfType(implementationType);
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

        // Assert
        _services.Should().NotContain(descriptor);

        object service = GetService(serviceType);
        _mapper.Received(1).MapScoped(Arg.Any<IServiceProvider>(), service, Arg.Is(ServiceToken(TypeId)));
        service.Should().BeSameAs(implementationInstance);
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

        // Assert
        _services.Should().NotContain(descriptor);

        object service = GetService(serviceType);
        _mapper.Received(1).MapTransient(Arg.Any<IServiceProvider>(), service, Arg.Is(ServiceToken(TypeId)));
        service.Should().BeOfType(implementationType);
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

        // Assert
        _services.Should().NotContain(descriptor);

        object service = GetService(serviceType);
        _mapper.Received(1).MapTransient(Arg.Any<IServiceProvider>(), service, Arg.Is(ServiceToken(TypeId)));
        service.Should().BeSameAs(implementationInstance);
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

        // Assert
        _services.Should().NotContain(descriptor);

        object service = GetKeyedService(serviceType, _serviceKey);
        _mapper.Received(1).MapSingleton(Arg.Any<IServiceProvider>(), service, Arg.Is(KeyedServiceToken(TypeId, _serviceKey)));
        service.Should().BeOfType(implementationType);
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

        // Assert
        _services.Should().NotContain(descriptor);

        object service = GetKeyedService(serviceType, _serviceKey);
        _mapper.Received(1).MapSingleton(Arg.Any<IServiceProvider>(), service, Arg.Is(KeyedServiceToken(TypeId, _serviceKey)));
        service.Should().BeSameAs(implementationInstance);
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

        // Assert
        _services.Should().NotContain(descriptor);

        object service = GetKeyedService(serviceType, _serviceKey);
        _mapper.Received(1).MapSingleton(Arg.Any<IServiceProvider>(), service, Arg.Is(KeyedServiceToken(TypeId, _serviceKey)));
        service.Should().BeSameAs(implementationInstance);
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

        // Assert
        _services.Should().NotContain(descriptor);

        object service = GetKeyedService(serviceType, _serviceKey);
        _mapper.Received(1).MapScoped(Arg.Any<IServiceProvider>(), service, Arg.Is(KeyedServiceToken(TypeId, _serviceKey)));
        service.Should().BeOfType(implementationType);
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

        // Assert
        _services.Should().NotContain(descriptor);

        object service = GetKeyedService(serviceType, _serviceKey);
        _mapper.Received(1).MapScoped(Arg.Any<IServiceProvider>(), service, Arg.Is(KeyedServiceToken(TypeId, _serviceKey)));
        service.Should().BeSameAs(implementationInstance);
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

        // Assert
        _services.Should().NotContain(descriptor);

        object service = GetKeyedService(serviceType, _serviceKey);
        _mapper.Received(1).MapTransient(Arg.Any<IServiceProvider>(), service, Arg.Is(KeyedServiceToken(TypeId, _serviceKey)));
        service.Should().BeOfType(implementationType);
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

        // Assert
        _services.Should().NotContain(descriptor);

        object service = GetKeyedService(serviceType, _serviceKey);
        _mapper.Received(1).MapTransient(Arg.Any<IServiceProvider>(), service, Arg.Is(KeyedServiceToken(TypeId, _serviceKey)));
        service.Should().BeSameAs(implementationInstance);
    }

    [Fact]
    public void Replace_HandlesImplementationTypeWithAllKindsOfDependencies()
    {
        // Arrange
        string key = "key";
        Dependency1 dependency1 = new();
        Dependency2 dependency2 = new();
        Dependency3 dependency3 = new();
        Dependency4 dependency4 = new();

        ServiceDescriptor descriptor1 = ServiceDescriptor.Singleton(dependency1);
        ServiceDescriptor descriptor2 = ServiceDescriptor.KeyedSingleton("dep2", dependency2);
        ServiceDescriptor descriptor3 = ServiceDescriptor.Singleton(dependency3);
        ServiceDescriptor descriptor4 = ServiceDescriptor.KeyedSingleton("dep4", dependency4);
        ServiceDescriptor targetDescriptor = ServiceDescriptor.KeyedSingleton<ServiceWithAllKindsOfDependencies, ServiceWithAllKindsOfDependencies>(key);

        IServiceRegister register = Substitute.For<IServiceRegister>();

        register
            .IsDAsyncService(Arg.Any<Type>())
            .Returns(call =>
            {
                Type serviceType = call.Arg<Type>();
                return serviceType == typeof(Dependency3) || serviceType == typeof(Dependency4);
            });

        _services.AddSingleton(register);
        _services.Add(descriptor1);
        _services.Add(descriptor2);
        _services.Add(descriptor3);
        _services.Add(descriptor4);
        _services.Add(targetDescriptor);

        // Act
        _sut.Replace(descriptor3);
        _sut.Replace(descriptor4);
        _sut.Replace(targetDescriptor);

        // Assert
        var result = GetKeyedService(typeof(ServiceWithAllKindsOfDependencies), key).Should().BeOfType<ServiceWithAllKindsOfDependencies>().Subject;
        result.Key.Should().Be(key);
        result.Dependency1.Should().Be(dependency1);
        result.Dependency2.Should().Be(dependency2);
        result.Dependency3.Should().Be(dependency3);
        result.Dependency4.Should().Be(dependency4);
        result.Dependency5.Should().BeNull();
        result.Dependency6.Should().BeNull();
        result.Dependency7.Should().BeNull();
        result.Dependency8.Should().BeNull();
    }

    private object GetService(Type serviceType) => GetServiceProvider().GetService(serviceType)!;

    private object GetKeyedService(Type serviceType, object serviceKey) => GetServiceProvider().GetKeyedService(serviceType, serviceKey)!;

    private IKeyedServiceProvider GetServiceProvider()
    {
        IServiceProvider provider = _services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        return (IKeyedServiceProvider)provider.CreateScope().ServiceProvider;
    }
}
