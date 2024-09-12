namespace DTasks.Extensions.Microsoft.DependencyInjection;

public class ServiceResolverBuilderTests
{
    private readonly ServiceResolverBuilder _sut = new();

    [Fact]
    public void Resolver_ReturnsFalseForUnknownTypeId()
    {
        // Arrange
        ServiceTypeId typeId = new("id");

        // Act
        ServiceResolver resolver = _sut.BuildServiceResolver();

        // Assert
        resolver(typeId, out Type? serviceType).Should().BeFalse();
        serviceType.Should().BeNull();
    }

    [Fact]
    public void Resolver_ReturnsTrueForKnownTypeId()
    {
        // Arrange
        Type type = typeof(object);

        // Act
        ServiceTypeId typeId = _sut.AddServiceType(type);
        ServiceResolver resolver = _sut.BuildServiceResolver();

        // Assert
        resolver(typeId, out Type? serviceType).Should().BeTrue();
        serviceType.Should().Be(type);
    }
}
