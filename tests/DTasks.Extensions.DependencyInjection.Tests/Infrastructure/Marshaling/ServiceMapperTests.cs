namespace DTasks.Extensions.DependencyInjection.Infrastructure.Marshaling;

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
        var surrogate = Substitute.For<ServiceSurrogate>();

        provider
            .GetService(typeof(IRootServiceMapper))
            .Returns(mapper);

        // Act
        _sut.MapSingleton(provider, service, surrogate);

        // Assert
        mapper.Received(1).MapService(service, surrogate);
    }

    [Fact]
    public void MarkScoped_UsesServiceMarker()
    {
        // Arrange
        var provider = Substitute.For<IServiceProvider>();
        var mapper = Substitute.For<IChildServiceMapper>();
        var service = new object();
        var surrogate = Substitute.For<ServiceSurrogate>();

        provider
            .GetService(typeof(IChildServiceMapper))
            .Returns(mapper);

        // Act
        _sut.MapScoped(provider, service, surrogate);

        // Assert
        mapper.Received(1).MapService(service, surrogate);
    }

    [Fact]
    public void MarkTransient_UsesRootServiceMarker_WhenResolvedAsPartOfSingletonService()
    {
        // Arrange
        var provider = _rootProvider;
        var mapper = Substitute.For<IRootServiceMapper>();
        var service = new object();
        var surrogate = Substitute.For<ServiceSurrogate>();

        provider
            .GetService(typeof(IRootServiceMapper))
            .Returns(mapper);

        // Act
        _sut.MapTransient(provider, service, surrogate);

        // Assert
        mapper.Received(1).MapService(service, surrogate);
    }

    [Fact]
    public void MarkTransient_UsesServiceMarker_WhenResolvedAsPartOfScopedService()
    {
        // Arrange
        var provider = Substitute.For<IServiceProvider>();
        var mapper = Substitute.For<IChildServiceMapper>();
        var service = new object();
        var surrogate = Substitute.For<ServiceSurrogate>();

        provider
            .GetService(typeof(IChildServiceMapper))
            .Returns(mapper);

        // Act
        _sut.MapTransient(provider, service, surrogate);

        // Assert
        mapper.Received(1).MapService(service, surrogate);
    }
}
