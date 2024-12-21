using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.Microsoft.Extensions.DependencyInjection;

public class DTasksServiceProviderExtensionsTests
{
    private readonly IKeyedServiceProvider _provider;
    private readonly IDAsyncServiceRegister _register;

    public DTasksServiceProviderExtensionsTests()
    {
        _provider = Substitute.For<IKeyedServiceProvider, ISupportRequiredService>();
        _register = Substitute.For<IDAsyncServiceRegister>();

        SupportRequiredService
            .GetRequiredService(typeof(IDAsyncServiceRegister))
            .Returns(_register);

        _register
            .IsDAsyncService(Arg.Any<Type>())
            .Returns(true);
    }

    private ISupportRequiredService SupportRequiredService => (ISupportRequiredService)_provider;

    [Fact]
    public void GetDAsyncService_CallsGetServiceAndEnsuresIsDAsync_WhenServiceIsNotNull()
    {
        // Arrange
        Type serviceType = typeof(object);
        object service = new();

        _provider
            .GetService(serviceType)
            .Returns(service);

        // Act
        object? dAsyncService = _provider.GetDAsyncService(serviceType);

        // Assert
        dAsyncService.Should().BeSameAs(service);
        _provider.Received(1).GetService(serviceType);
        _register.Received(1).IsDAsyncService(serviceType);
    }

    [Fact]
    public void GetDAsyncService_CallsGetServiceAndDoesNotEnsuresIsDAsync_WhenServiceIsNull()
    {
        // Arrange
        Type serviceType = typeof(object);

        _provider
            .GetService(serviceType)
            .Returns(null);

        // Act
        object? dAsyncService = _provider.GetDAsyncService(serviceType);

        // Assert
        dAsyncService.Should().BeNull();
        _provider.Received(1).GetService(serviceType);
        _register.DidNotReceive().IsDAsyncService(serviceType);
    }

    [Fact]
    public void GetDAsyncService_ThrowsArgumentException_WhenServiceIsNotDAsyn()
    {
        // Arrange
        Type serviceType = typeof(object);
        object service = new();

        _provider
            .GetService(serviceType)
            .Returns(service);

        _register
            .IsDAsyncService(serviceType)
            .Returns(false);

        // Act
        Action act = () => _provider.GetDAsyncService(serviceType);

        // Assert
        act.Should().ThrowExactly<ArgumentException>().Which.ParamName.Should().Be("serviceType");
    }

    [Fact]
    public void GetDAsyncService_Typed_CallsGetServiceAndEnsuresIsDAsync_WhenServiceIsNotNull()
    {
        // Arrange
        Type serviceType = typeof(object);
        object service = new();

        _provider
            .GetService(serviceType)
            .Returns(service);

        // Act
        object? dAsyncService = _provider.GetDAsyncService<object>();

        // Assert
        dAsyncService.Should().BeSameAs(service);
        _provider.Received(1).GetService(serviceType);
        _register.Received(1).IsDAsyncService(serviceType);
    }

    [Fact]
    public void GetDAsyncService_Typed_CallsGetServiceAndDoesNotEnsuresIsDAsync_WhenServiceIsNull()
    {
        // Arrange
        Type serviceType = typeof(object);

        _provider
            .GetService(serviceType)
            .Returns(null);

        // Act
        object? dAsyncService = _provider.GetDAsyncService<object>();

        // Assert
        dAsyncService.Should().BeNull();
        _provider.Received(1).GetService(serviceType);
        _register.DidNotReceive().IsDAsyncService(serviceType);
    }

    [Fact]
    public void GetDAsyncService_Typed_ThrowsArgumentException_WhenServiceIsNotDAsyn()
    {
        // Arrange
        Type serviceType = typeof(object);
        object service = new();

        _provider
            .GetService(serviceType)
            .Returns(service);

        _register
            .IsDAsyncService(serviceType)
            .Returns(false);

        // Act
        Action act = () => _provider.GetDAsyncService<object>();

        // Assert
        act.Should().ThrowExactly<ArgumentException>().Which.ParamName.Should().Be("typeof(T)");
    }

    [Fact]
    public void GetRequiredDAsyncService_CallsGetRequiredServiceAndEnsuresIsDAsync()
    {
        // Arrange
        Type serviceType = typeof(object);
        object service = new();

        SupportRequiredService
            .GetRequiredService(serviceType)
            .Returns(service);

        // Act
        object dAsyncService = _provider.GetRequiredDAsyncService(serviceType);

        // Assert
        dAsyncService.Should().BeSameAs(service);
        SupportRequiredService.Received(1).GetRequiredService(serviceType);
        _register.Received(1).IsDAsyncService(serviceType);
    }

    [Fact]
    public void GetRequiredDAsyncService_Typed_CallsGetRequiredServiceAndEnsuresIsDAsync()
    {
        // Arrange
        Type serviceType = typeof(object);
        object service = new();

        SupportRequiredService
            .GetRequiredService(serviceType)
            .Returns(service);

        // Act
        object dAsyncService = _provider.GetRequiredDAsyncService<object>();

        // Assert
        dAsyncService.Should().BeSameAs(service);
        SupportRequiredService.Received(1).GetRequiredService(serviceType);
        _register.Received(1).IsDAsyncService(serviceType);
    }

    [Fact]
    public void GetDAsyncServices_CallsGetRequiredServiceAndEnsuresIsDAsync()
    {
        // Arrange
        Type serviceType = typeof(object);
        Type enumerableType = typeof(IEnumerable<>).MakeGenericType(serviceType);
        object[] services = [new()];

        SupportRequiredService
            .GetRequiredService(enumerableType)
            .Returns(services);

        // Act
        IEnumerable<object?> dAsyncServices = _provider.GetDAsyncServices(serviceType);

        // Assert
        dAsyncServices.Should().BeSameAs(services);
        SupportRequiredService.Received(1).GetRequiredService(enumerableType);
        _register.Received(1).IsDAsyncService(serviceType);
    }

    [Fact]
    public void GetDAsyncServices_Typed_CallsGetRequiredServiceAndEnsuresIsDAsync()
    {
        // Arrange
        Type serviceType = typeof(object);
        Type enumerableType = typeof(IEnumerable<>).MakeGenericType(serviceType);
        object[] services = [new()];

        SupportRequiredService
            .GetRequiredService(enumerableType)
            .Returns(services);

        // Act
        IEnumerable<object> dAsyncServices = _provider.GetDAsyncServices<object>();

        // Assert
        dAsyncServices.Should().BeSameAs(services);
        SupportRequiredService.Received(1).GetRequiredService(enumerableType);
        _register.Received(1).IsDAsyncService(serviceType);
    }




    [Fact]
    public void GetKeyedDAsyncService_CallsGetKeyedServiceAndEnsuresIsDAsync_WhenServiceIsNotNull()
    {
        // Arrange
        Type serviceType = typeof(object);
        object serviceKey = new();
        object service = new();

        _provider
            .GetKeyedService(serviceType, serviceKey)
            .Returns(service);

        // Act
        object? dAsyncService = _provider.GetKeyedDAsyncService(serviceType, serviceKey);

        // Assert
        dAsyncService.Should().BeSameAs(service);
        _provider.Received(1).GetKeyedService(serviceType, serviceKey);
        _register.Received(1).IsDAsyncService(serviceType);
    }

    [Fact]
    public void GetKeyedDAsyncService_CallsGetKeyedServiceAndDoesNotEnsuresIsDAsync_WhenServiceIsNull()
    {
        // Arrange
        Type serviceType = typeof(object);
        object serviceKey = new();

        _provider
            .GetKeyedService(serviceType, serviceKey)
            .Returns(null);

        // Act
        object? dAsyncService = _provider.GetKeyedDAsyncService(serviceType, serviceKey);

        // Assert
        dAsyncService.Should().BeNull();
        _provider.Received(1).GetKeyedService(serviceType, serviceKey);
        _register.DidNotReceive().IsDAsyncService(serviceType);
    }

    [Fact]
    public void GetKeyedDAsyncService_Typed_CallsGetKeyedServiceAndEnsuresIsDAsync_WhenServiceIsNotNull()
    {
        // Arrange
        Type serviceType = typeof(object);
        object serviceKey = new();
        object service = new();

        _provider
            .GetKeyedService(serviceType, serviceKey)
            .Returns(service);

        // Act
        object? dAsyncService = _provider.GetKeyedDAsyncService<object>(serviceKey);

        // Assert
        dAsyncService.Should().BeSameAs(service);
        _provider.Received(1).GetKeyedService(serviceType, serviceKey);
        _register.Received(1).IsDAsyncService(serviceType);
    }

    [Fact]
    public void GetKeyedDAsyncService_Typed_CallsGetKeyedServiceAndDoesNotEnsuresIsDAsync_WhenServiceIsNull()
    {
        // Arrange
        Type serviceType = typeof(object);
        object serviceKey = new();

        _provider
            .GetKeyedService(serviceType, serviceKey)
            .Returns(null);

        // Act
        object? dAsyncService = _provider.GetKeyedDAsyncService<object>(serviceKey);

        // Assert
        dAsyncService.Should().BeNull();
        _provider.Received(1).GetKeyedService(serviceType, serviceKey);
        _register.DidNotReceive().IsDAsyncService(serviceType);
    }

    [Fact]
    public void GetRequiredKeyedDAsyncService_CallsGetRequiredKeyedServiceAndEnsuresIsDAsync()
    {
        // Arrange
        Type serviceType = typeof(object);
        object serviceKey = new();
        object service = new();

        _provider
            .GetRequiredKeyedService(serviceType, serviceKey)
            .Returns(service);

        // Act
        object dAsyncService = _provider.GetRequiredKeyedDAsyncService(serviceType, serviceKey);

        // Assert
        dAsyncService.Should().BeSameAs(service);
        _provider.Received(1).GetRequiredKeyedService(serviceType, serviceKey);
        _register.Received(1).IsDAsyncService(serviceType);
    }

    [Fact]
    public void GetRequiredKeyedDAsyncService_Typed_CallsGetRequiredKeyedServiceAndEnsuresIsDAsync()
    {
        // Arrange
        Type serviceType = typeof(object);
        object serviceKey = new();
        object service = new();

        _provider
            .GetRequiredKeyedService(serviceType, serviceKey)
            .Returns(service);

        // Act
        object dAsyncService = _provider.GetRequiredKeyedDAsyncService<object>(serviceKey);

        // Assert
        dAsyncService.Should().BeSameAs(service);
        _provider.Received(1).GetRequiredKeyedService(serviceType, serviceKey);
        _register.Received(1).IsDAsyncService(serviceType);
    }

    [Fact]
    public void GetKeyedDAsyncServices_CallsGetRequiredKeyedServiceAndEnsuresIsDAsync()
    {
        // Arrange
        Type serviceType = typeof(object);
        Type enumerableType = typeof(IEnumerable<>).MakeGenericType(serviceType);
        object serviceKey = new();
        object[] services = [new()];

        _provider
            .GetRequiredKeyedService(enumerableType, serviceKey)
            .Returns(services);

        // Act
        IEnumerable<object?> dAsyncServices = _provider.GetKeyedDAsyncServices(serviceType, serviceKey);

        // Assert
        dAsyncServices.Should().BeSameAs(services);
        _provider.Received(1).GetRequiredKeyedService(enumerableType, serviceKey);
        _register.Received(1).IsDAsyncService(serviceType);
    }

    [Fact]
    public void GetKeyedDAsyncServices_Typed_CallsGetRequiredKeyedServiceAndEnsuresIsDAsync()
    {
        // Arrange
        Type serviceType = typeof(object);
        Type enumerableType = typeof(IEnumerable<>).MakeGenericType(serviceType);
        object serviceKey = new();
        object[] services = [new()];

        _provider
            .GetRequiredKeyedService(enumerableType, serviceKey)
            .Returns(services);

        // Act
        IEnumerable<object> dAsyncServices = _provider.GetKeyedDAsyncServices<object>(serviceKey);

        // Assert
        dAsyncServices.Should().BeSameAs(services);
        _provider.Received(1).GetRequiredKeyedService(enumerableType, serviceKey);
        _register.Received(1).IsDAsyncService(serviceType);
    }
}
