using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.DependencyInjection.Configuration;

public class ServiceConfigurationBuilderTests
{
    private readonly ServiceConfigurationBuilder _sut;

    public ServiceConfigurationBuilderTests()
    {
        _sut = new ServiceConfigurationBuilder();
    }

    [Fact]
    public void IsDAsyncService_ReturnsFalse_WhenServiceTypeWasNotRegistered()
    {
        // Arrange
        ServiceDescriptor descriptor = ServiceDescriptor.Singleton(new object());

        // Act
        bool result = _sut.IsDAsyncService(descriptor);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsDAsyncService_ReturnsTrue_WhenServiceTypeWasRegistered()
    {
        // Arrange
        Type serviceType = typeof(object);
        ServiceDescriptor descriptor = ServiceDescriptor.Singleton(serviceType, new object());

        RegisterDAsyncService(serviceType);

        // Act
        bool result = _sut.IsDAsyncService(descriptor);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsDAsyncService_ReturnsFalse_WhenKeyedServiceTypeWasRegisteredWithDifferentKey()
    {
        // Arrange
        Type serviceType = typeof(object);
        object serviceKey = new object();
        ServiceDescriptor descriptor = ServiceDescriptor.KeyedSingleton(serviceType, serviceKey);

        RegisterDAsyncService(serviceType, new object());

        // Act
        bool result = _sut.IsDAsyncService(descriptor);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsDAsyncService_ReturnsTrue_WhenKeyedServiceTypeWasRegistered()
    {
        // Arrange
        Type serviceType = typeof(object);
        object serviceKey = new object();
        ServiceDescriptor descriptor = ServiceDescriptor.KeyedSingleton(serviceType, serviceKey, new object());

        RegisterDAsyncService(serviceType, serviceKey);

        // Act
        bool result = _sut.IsDAsyncService(descriptor);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsDAsyncService_ReturnsTrue_WhenAllServicesWereRegistered()
    {
        // Arrange
        ServiceDescriptor descriptor = ServiceDescriptor.Singleton(new object());

        RegisterAllServices();

        // Act
        bool result = _sut.IsDAsyncService(descriptor);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsDAsyncService_ReturnsFalse_WhenAllServicesWereRegisteredAndServiceTypeIsOpenGeneric()
    {
        // Arrange
        Type serviceType = typeof(IEnumerable<>);
        ServiceDescriptor descriptor = ServiceDescriptor.Singleton(serviceType, new object());

        RegisterAllServices();

        // Act
        bool result = _sut.IsDAsyncService(descriptor);

        // Assert
        result.Should().BeFalse();
    }

    private void RegisterDAsyncService(Type serviceType)
    {
        ((IServiceConfigurationBuilder)_sut).RegisterDAsyncService(serviceType);
    }

    private void RegisterDAsyncService(Type serviceType, object serviceKey)
    {
        ((IServiceConfigurationBuilder)_sut).RegisterDAsyncService(serviceType, serviceKey);
    }

    private void RegisterAllServices()
    {
        ((IServiceConfigurationBuilder)_sut).RegisterAllServices();
    }
}
