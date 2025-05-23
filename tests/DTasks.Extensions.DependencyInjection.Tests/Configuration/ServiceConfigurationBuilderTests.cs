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
        ServiceDescriptor descriptor = ServiceDescriptor.Singleton(new object());

        RegisterDAsyncService<object>();

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
        object otherKey = new object();
        ServiceDescriptor descriptor = ServiceDescriptor.KeyedSingleton(serviceType, serviceKey);

        RegisterDAsyncService<object>(otherKey);

        // Act
        bool result = _sut.IsDAsyncService(descriptor);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsDAsyncService_ReturnsTrue_WhenKeyedServiceTypeWasRegistered()
    {
        // Arrange
        object serviceKey = new object();
        ServiceDescriptor descriptor = ServiceDescriptor.KeyedSingleton(serviceKey, new object());

        RegisterDAsyncService<object>(serviceKey);

        // Act
        bool result = _sut.IsDAsyncService(descriptor);

        // Assert
        result.Should().BeTrue();
    }

    private void RegisterDAsyncService<TService>()
    {
        ((IServiceConfigurationBuilder)_sut).RegisterDAsyncService<TService>();
    }

    private void RegisterDAsyncService<TService>(object serviceKey)
    {
        ((IServiceConfigurationBuilder)_sut).RegisterDAsyncService<TService>(serviceKey);
    }
}
