using DTasks.Extensions.Microsoft.DependencyInjection.Hosting;

namespace DTasks.Extensions.Microsoft.DependencyInjection;

public class LifetimeServiceMapperTests
{
    private readonly IServiceProvider _applicationServices;
    private readonly ServiceMapper _sut;

    public LifetimeServiceMapperTests()
    {
        _applicationServices = Substitute.For<IServiceProvider>();
        _sut = new ServiceMapper(_applicationServices);
    }

    [Fact]
    public void MarkSingleton_UsesRootServiceMarker()
    {
        // Arrange
        var services = Substitute.For<IServiceProvider>();
        var mapper = Substitute.For<IRootServiceMapper>();
        var service = new object();
        var token = Substitute.For<ServiceToken>();

        services
            .GetService(typeof(IRootServiceMapper))
            .Returns(mapper);

        // Act
        _sut.MapSingleton(services, service, token);

        // Assert
        mapper.Received(1).MapService(service, token);
    }

    [Fact]
    public void MarkScoped_UsesServiceMarker()
    {
        // Arrange
        var services = Substitute.For<IServiceProvider>();
        var mapper = Substitute.For<IChildServiceMapper>();
        var service = new object();
        var token = Substitute.For<ServiceToken>();

        services
            .GetService(typeof(IChildServiceMapper))
            .Returns(mapper);

        // Act
        _sut.MapScoped(services, service, token);

        // Assert
        mapper.Received(1).MapService(service, token);
    }

    [Fact]
    public void MarkTransient_UsesRootServiceMarker_WhenResolvedAsPartOfSingletonService()
    {
        // Arrange
        var services = _applicationServices;
        var mapper = Substitute.For<IRootServiceMapper>();
        var service = new object();
        var token = Substitute.For<ServiceToken>();

        services
            .GetService(typeof(IRootServiceMapper))
            .Returns(mapper);

        // Act
        _sut.MapTransient(services, service, token);

        // Assert
        mapper.Received(1).MapService(service, token);
    }

    [Fact]
    public void MarkTransient_UsesServiceMarker_WhenResolvedAsPartOfScopedService()
    {
        // Arrange
        var services = Substitute.For<IServiceProvider>();
        var mapper = Substitute.For<IChildServiceMapper>();
        var service = new object();
        var token = Substitute.For<ServiceToken>();

        services
            .GetService(typeof(IChildServiceMapper))
            .Returns(mapper);

        // Act
        _sut.MapTransient(services, service, token);

        // Assert
        mapper.Received(1).MapService(service, token);
    }
}
