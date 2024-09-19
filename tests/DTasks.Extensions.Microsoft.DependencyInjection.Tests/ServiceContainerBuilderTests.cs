using DTasks.Extensions.Microsoft.DependencyInjection.Hosting;
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
    public void Intercept_ReplacesSingletonWithImplementationType(Type serviceType, Type implementationType)
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
    public void Intercept_ReplacesSingletonWithImplementationInstance(Type serviceType, object implementationInstance)
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
    public void Intercept_ReplacesSingletonWithImplementationFactory(Type serviceType, object implementationInstance)
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
    public void Intercept_ReplacesScopedWithImplementationType(Type serviceType, Type implementationType)
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
    public void Intercept_ReplacesScopedWithImplementationFactory(Type serviceType, object implementationInstance)
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
    public void Intercept_ReplacesTransientWithImplementationType(Type serviceType, Type implementationType)
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
    public void Intercept_ReplacesTransientWithImplementationFactory(Type serviceType, object implementationInstance)
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
    public void Intercept_ReplacesSingletonWithKeyedImplementationType(Type serviceType, Type implementationType)
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
    public void Intercept_ReplacesSingletonWithKeyedImplementationInstance(Type serviceType, object implementationInstance)
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
    public void Intercept_ReplacesSingletonWithKeyedImplementationFactory(Type serviceType, object implementationInstance)
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
    public void Intercept_ReplacesScopedWithKeyedImplementationType(Type serviceType, Type implementationType)
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
    public void Intercept_ReplacesScopedWithKeyedImplementationFactory(Type serviceType, object implementationInstance)
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
    public void Intercept_ReplacesTransientWithKeyedImplementationType(Type serviceType, Type implementationType)
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
    public void Intercept_ReplacesTransientWithKeyedImplementationFactory(Type serviceType, object implementationInstance)
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
