using DTasks.Infrastructure.Marshaling;

namespace DTasks.Extensions.DependencyInjection;

public class DAsyncServiceRegisterBuilderTests
{
    private readonly IDAsyncTypeResolverBuilder _typeResolverBuilder;
    private readonly IDAsyncTypeResolver _typeResolver;
    private readonly DAsyncServiceRegisterBuilder _sut;

    public DAsyncServiceRegisterBuilderTests()
    {
        _typeResolverBuilder = Substitute.For<IDAsyncTypeResolverBuilder>();
        _typeResolver = Substitute.For<IDAsyncTypeResolver>();
        _sut = new DAsyncServiceRegisterBuilder(_typeResolverBuilder);
    }

    [Fact]
    public void IsDAsyncService_ReturnsFalse_WhenTypeIdIsUnknown()
    {
        // Arrange
        TypeId typeId = new("id");

        // Act
        IDAsyncServiceRegister register = _sut.Build(_typeResolver);
        bool isService = register.IsDAsyncService(typeId, out Type? serviceType);

        // Assert
        isService.Should().BeFalse();
        serviceType.Should().BeNull();
    }

    [Fact]
    public void IsDAsyncService_ReturnsFalse_WhenServiceTypeIsUnknown()
    {
        // Arrange
        Type serviceType = typeof(object);

        // Act
        IDAsyncServiceRegister register = _sut.Build(_typeResolver);
        bool isService = register.IsDAsyncService(serviceType);

        // Assert
        isService.Should().BeFalse();
    }

    [Fact]
    public void IsDAsyncService_ReturnsTrue_WhenTypeIdIsKnown()
    {
        // Arrange
        Type type = typeof(object);
        TypeId typeId = new("id");

        _typeResolverBuilder.Register(type).Returns(typeId);
        _typeResolver.GetType(typeId).Returns(type);

        // Act
        TypeId actualTypeId = _sut.AddServiceType(type);
        IDAsyncServiceRegister register = _sut.Build(_typeResolver);
        bool isService = register.IsDAsyncService(typeId, out Type? serviceType);

        // Assert
        isService.Should().BeTrue();
        serviceType.Should().Be(type);
        actualTypeId.Should().Be(typeId);
    }

    [Fact]
    public void IsDAsyncService_ReturnsTrue_WhenServiceTypeIsKnown()
    {
        // Arrange
        Type serviceType = typeof(object);

        // Act
        _sut.AddServiceType(serviceType);
        IDAsyncServiceRegister register = _sut.Build(_typeResolver);
        bool isService = register.IsDAsyncService(serviceType);

        // Assert
        isService.Should().BeTrue();
    }
}
