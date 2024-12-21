using DTasks.Extensions.Microsoft.Extensions.DependencyInjection.Marshaling;

namespace DTasks.Extensions.Microsoft.Extensions.DependencyInjection.Mapping;

public class ServiceMapperTests
{
    private readonly IServiceProvider _rootProvider;
    private readonly ServiceMapper _sut;

    public ServiceMapperTests()
    {
        _rootProvider = Substitute.For<IServiceProvider>();
        _sut = new ServiceMapper(_rootProvider);
    }

    [Fact]
    public void MarkSingleton_UsesRootServiceMarker()
    {
        // Arrange
        var provider = Substitute.For<IServiceProvider>();
        var mapper = Substitute.For<IRootServiceMapper>();
        var service = new object();
        var token = Substitute.For<ServiceToken>();

        provider
            .GetService(typeof(IRootServiceMapper))
            .Returns(mapper);

        // Act
        _sut.MapSingleton(provider, service, token);

        // Assert
        mapper.Received(1).MapService(service, token);
    }

    [Fact]
    public void MarkScoped_UsesServiceMarker()
    {
        // Arrange
        var provider = Substitute.For<IServiceProvider>();
        var mapper = Substitute.For<IChildServiceMapper>();
        var service = new object();
        var token = Substitute.For<ServiceToken>();

        provider
            .GetService(typeof(IChildServiceMapper))
            .Returns(mapper);

        // Act
        _sut.MapScoped(provider, service, token);

        // Assert
        mapper.Received(1).MapService(service, token);
    }

    [Fact]
    public void MarkTransient_UsesRootServiceMarker_WhenResolvedAsPartOfSingletonService()
    {
        // Arrange
        var provider = _rootProvider;
        var mapper = Substitute.For<IRootServiceMapper>();
        var service = new object();
        var token = Substitute.For<ServiceToken>();

        provider
            .GetService(typeof(IRootServiceMapper))
            .Returns(mapper);

        // Act
        _sut.MapTransient(provider, service, token);

        // Assert
        mapper.Received(1).MapService(service, token);
    }

    [Fact]
    public void MarkTransient_UsesServiceMarker_WhenResolvedAsPartOfScopedService()
    {
        // Arrange
        var provider = Substitute.For<IServiceProvider>();
        var mapper = Substitute.For<IChildServiceMapper>();
        var service = new object();
        var token = Substitute.For<ServiceToken>();

        provider
            .GetService(typeof(IChildServiceMapper))
            .Returns(mapper);

        // Act
        _sut.MapTransient(provider, service, token);

        // Assert
        mapper.Received(1).MapService(service, token);
    }
}
