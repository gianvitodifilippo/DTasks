namespace DTasks.Extensions.Microsoft.DependencyInjection;

public class DAsyncServiceRegisterBuilderTests
{
    private readonly DAsyncServiceRegisterBuilder _sut;

    public DAsyncServiceRegisterBuilderTests()
    {
        _sut = new DAsyncServiceRegisterBuilder();
    }

    [Fact]
    public void IsDAsyncService_ReturnsFalse_WhenTypeIdIsUnknown()
    {
        // Arrange
        ServiceTypeId typeId = new("id");

        // Act
        IDAsyncServiceRegister register = _sut.Build();
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
        IDAsyncServiceRegister register = _sut.Build();
        bool isService = register.IsDAsyncService(serviceType);

        // Assert
        isService.Should().BeFalse();
    }

    [Fact]
    public void IsDAsyncService_ReturnsTrue_WhenTypeIdIsKnown()
    {
        // Arrange
        Type type = typeof(object);

        // Act
        ServiceTypeId typeId = _sut.AddServiceType(type);
        IDAsyncServiceRegister register = _sut.Build();
        bool isService = register.IsDAsyncService(typeId, out Type? serviceType);

        // Assert
        isService.Should().BeTrue();
        serviceType.Should().Be(type);
    }

    [Fact]
    public void IsDAsyncService_ReturnsTrue_WhenServiceTypeIsKnown()
    {
        // Arrange
        Type serviceType = typeof(object);

        // Act
        _sut.AddServiceType(serviceType);
        IDAsyncServiceRegister register = _sut.Build();
        bool isService = register.IsDAsyncService(serviceType);

        // Assert
        isService.Should().BeTrue();
    }
}
