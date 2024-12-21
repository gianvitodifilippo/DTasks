using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTasks.Extensions.Microsoft.Extensions.DependencyInjection;

public partial class DTasksServiceConfigurationTests
{
    private readonly IServiceCollection _services;
    private readonly IServiceContainerBuilder _containerBuilder;
    private readonly DTasksServiceConfiguration _sut;

    public DTasksServiceConfigurationTests()
    {
        _services = new ServiceCollection();
        _containerBuilder = Substitute.For<IServiceContainerBuilder>();
        _sut = new DTasksServiceConfiguration(_services);
    }

    [Fact]
    public void Services_ReturnsSameServiceCollectionPassedInConstructor()
    {
        _sut.Services.Should().BeSameAs(_services);
    }

    [Fact]
    public void ReplaceDAsyncServices_ReplacesServicesThatHaveDTaskMethods()
    {
        // Arrange
        Type serviceType = typeof(IServiceWithDTaskMethod);
        _services.AddSingleton(serviceType);

        // Act
        _sut.ReplaceDAsyncServices(_containerBuilder);

        // Assert
        _containerBuilder.Received(1).Replace(Arg.Is<ServiceDescriptor>(descriptor => descriptor.ServiceType == serviceType));
    }

    [Fact]
    public void ReplaceDAsyncServices_DoesNotReplaceServicesThatDoNotHaveDTaskMethods()
    {
        // Arrange
        Type serviceType = typeof(IServiceWithoutDTaskMethods);
        _services.AddSingleton(serviceType);

        // Act
        _sut.ReplaceDAsyncServices(_containerBuilder);

        // Assert
        _containerBuilder.DidNotReceive().Replace(Arg.Is<ServiceDescriptor>(descriptor => descriptor.ServiceType == serviceType));
    }

    [Fact]
    public void ReplaceDAsyncServices_ReplacesRegisteredServices()
    {
        // Arrange
        Type serviceType = typeof(IServiceWithoutDTaskMethods);
        _services.AddSingleton(serviceType);

        // Act
        _sut.RegisterDAsyncService(serviceType);
        _sut.ReplaceDAsyncServices(_containerBuilder);

        // Assert
        _containerBuilder.Received(1).Replace(Arg.Is<ServiceDescriptor>(descriptor => descriptor.ServiceType == serviceType));
    }

    [Fact]
    public void ReplaceDAsyncServices_ReplacesRegisteredKeyedServices()
    {
        // Arrange
        Type serviceType = typeof(IServiceWithoutDTaskMethods);
        object serviceKey = new();
        _services.AddKeyedSingleton(serviceType, serviceKey);

        // Act
        _sut.RegisterDAsyncService(serviceType, serviceKey);
        _sut.ReplaceDAsyncServices(_containerBuilder);

        // Assert
        _containerBuilder.Received(1).Replace(Arg.Is<ServiceDescriptor>(descriptor => descriptor.ServiceType == serviceType && descriptor.ServiceKey == serviceKey));
    }

    [Fact]
    public void ReplaceDAsyncServices_ReplacesRegisteredServicesWhenKeyIsNull()
    {
        // Arrange
        Type serviceType = typeof(IServiceWithoutDTaskMethods);
        _services.AddSingleton(serviceType);

        // Act
        _sut.RegisterDAsyncService(serviceType, null);
        _sut.ReplaceDAsyncServices(_containerBuilder);

        // Assert
        _containerBuilder.Received(1).Replace(Arg.Is<ServiceDescriptor>(descriptor => descriptor.ServiceType == serviceType));
    }


    [Fact]
    public void ReplaceDAsyncServices_ThrowsWhenServiceWithDTaskMethodIsOpenGeneric()
    {
        // Arrange
        Type serviceType = typeof(IServiceWithDTaskMethod<>);
        _services.AddSingleton(serviceType);

        // Act
        Action act = () => _sut.ReplaceDAsyncServices(_containerBuilder);

        // Assert
        act.Should().ThrowExactly<NotSupportedException>();
        _containerBuilder.DidNotReceive().Replace(Arg.Is<ServiceDescriptor>(descriptor => descriptor.ServiceType == serviceType));
    }

    [Fact]
    public void RegisterDAsyncService_ThrowsWhenServiceIsOpenGeneric()
    {
        // Arrange
        Type serviceType = typeof(IServiceWithoutDTaskMethods<>);
        _services.AddSingleton(serviceType);

        // Act
        Action act = () => _sut.RegisterDAsyncService(serviceType);

        // Assert
        act.Should().ThrowExactly<NotSupportedException>();
    }

    [Fact]
    public void RegisterDAsyncService_Keyed_ThrowsWhenServiceIsOpenGeneric()
    {
        // Arrange
        Type serviceType = typeof(IServiceWithoutDTaskMethods<>);
        object serviceKey = new();
        _services.AddKeyedSingleton(serviceType, serviceKey);

        // Act
        Action act = () => _sut.RegisterDAsyncService(serviceType, serviceKey);

        // Assert
        act.Should().ThrowExactly<NotSupportedException>();
    }

    [Fact]
    public void RegisterDAsyncService_ThrowsWhenServiceIsNotPartOfServiceCollection()
    {
        // Arrange
        Type serviceType = typeof(IServiceWithoutDTaskMethods);

        // Act
        Action act = () => _sut.RegisterDAsyncService(serviceType);

        // Assert
        act.Should().ThrowExactly<ArgumentException>().Which.ParamName.Should().Be("serviceType");
    }

    [Fact]
    public void RegisterDAsyncService_Keyed_ThrowsWhenServiceIsNotPartOfServiceCollection()
    {
        // Arrange
        Type serviceType = typeof(IServiceWithoutDTaskMethods);
        object serviceKey = new();

        // Act
        Action act = () => _sut.RegisterDAsyncService(serviceType, serviceKey);

        // Assert
        act.Should().ThrowExactly<ArgumentException>().Which.ParamName.Should().Be("serviceType");
    }

    [Fact]
    public void RegisterDAsyncService_Keyed_ThrowsWhenServiceIsNotKeyed()
    {
        // Arrange
        Type serviceType = typeof(IServiceWithoutDTaskMethods);
        object serviceKey = new();
        _services.AddSingleton(serviceType);

        // Act
        Action act = () => _sut.RegisterDAsyncService(serviceType, serviceKey);

        // Assert
        act.Should().ThrowExactly<ArgumentException>().Which.ParamName.Should().Be("serviceType");
    }
}
