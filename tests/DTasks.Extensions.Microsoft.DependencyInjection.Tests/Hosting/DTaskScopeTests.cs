using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.Microsoft.DependencyInjection.Hosting;

public class DTaskScopeTests
{
    private readonly IKeyedServiceProvider _provider;
    private readonly IDAsyncServiceRegister _register;

    public DTaskScopeTests()
    {
        _provider = Substitute.For<IKeyedServiceProvider>();
        _register = Substitute.For<IDAsyncServiceRegister>();
    }

    [Fact]
    public void TryGetReferenceToken_ReturnsFalse_WhenScopeIsRootAndReferenceWasNotMapped()
    {
        // Arrange
        object reference = new();

        RootDTaskScope sut = new(_provider, _register);

        // Act
        bool result = sut.TryGetReferenceToken(reference, out object? referenceToken);

        // Assert
        result.Should().BeFalse();
        referenceToken.Should().BeNull();
    }

    [Fact]
    public void TryGetReferenceToken_ReturnsTrue_WhenScopeIsRootAndReferenceWasMapped()
    {
        // Arrange
        object reference = new();
        ServiceToken token = Substitute.For<ServiceToken>();

        RootDTaskScope sut = new(_provider, _register);

        sut.MapService(reference, token);

        // Act
        bool result = sut.TryGetReferenceToken(reference, out object? referenceToken);

        // Assert
        result.Should().BeTrue();
        referenceToken.Should().BeSameAs(token);
    }

    [Fact]
    public void TryGetReferenceToken_ReturnsFalse_WhenScopeIsChildAndReferenceWasNotMapped()
    {
        // Arrange
        object reference = new();

        RootDTaskScope root = new(_provider, _register);
        ChildDTaskScope sut = new(_provider, _register, root);

        // Act
        bool result = sut.TryGetReferenceToken(reference, out object? referenceToken);

        // Assert
        result.Should().BeFalse();
        referenceToken.Should().BeNull();
    }

    [Fact]
    public void TryGetReferenceToken_ReturnsTrue_WhenScopeIsChildAndReferenceWasMappedInRoot()
    {
        // Arrange
        object reference = new();
        ServiceToken token = Substitute.For<ServiceToken>();

        RootDTaskScope root = new(_provider, _register);
        ChildDTaskScope sut = new(_provider, _register, root);

        root.MapService(reference, token);

        // Act
        bool result = sut.TryGetReferenceToken(reference, out object? referenceToken);

        // Assert
        result.Should().BeTrue();
        referenceToken.Should().BeSameAs(token);
    }

    [Fact]
    public void TryGetReferenceToken_ReturnsTrue_WhenScopeIsChildAndReferenceWasMappedInChild()
    {
        // Arrange
        object reference = new();
        ServiceToken token = Substitute.For<ServiceToken>();

        RootDTaskScope root = new(_provider, _register);
        ChildDTaskScope sut = new(_provider, _register, root);

        sut.MapService(reference, token);

        // Act
        bool result = sut.TryGetReferenceToken(reference, out object? referenceToken);

        // Assert
        result.Should().BeTrue();
        referenceToken.Should().BeSameAs(token);
    }

    [Fact]
    public void TryGetReference_ReturnsFalse_WhenTokenIsOfTheWrongType()
    {
        // Arrange
        object token = new();

        RootDTaskScope sut = new(_provider, _register);

        // Act
        bool result = sut.TryGetReference(token, out object? reference);

        // Assert
        result.Should().BeFalse();
        reference.Should().BeNull();
    }

    [Fact]
    public void TryGetReference_ReturnsFalse_WhenServiceIsNotDAsync()
    {
        // Arrange
        ServiceTypeId typeId = new ServiceTypeId("typeId");
        ServiceToken token = ServiceToken.Create(typeId);

        _register
            .IsDAsyncService(typeId, out Arg.Any<Type?>())
            .Returns(false);

        RootDTaskScope sut = new(_provider, _register);

        // Act
        bool result = sut.TryGetReference(token, out object? reference);

        // Assert
        result.Should().BeFalse();
        reference.Should().BeNull();
    }

    [Fact]
    public void TryGetReference_ReturnsTrue_WhenServiceIsDAsync()
    {
        // Arrange
        ServiceTypeId typeId = new ServiceTypeId("typeId");
        ServiceToken token = ServiceToken.Create(typeId);
        Type serviceType = typeof(object);
        object service = new();

        _register
            .IsDAsyncService(typeId, out Arg.Any<Type?>())
            .Returns(call =>
            {
                call[1] = serviceType;
                return true;
            });

        _provider
            .GetService(serviceType)
            .Returns(service);

        RootDTaskScope sut = new(_provider, _register);

        // Act
        bool result = sut.TryGetReference(token, out object? reference);

        // Assert
        result.Should().BeTrue();
        reference.Should().BeSameAs(service);
    }

    [Fact]
    public void TryGetReference_ReturnsTrue_WhenServiceIsDAsyncAndKeyed()
    {
        // Arrange
        ServiceTypeId typeId = new ServiceTypeId("typeId");
        object serviceKey = new();
        ServiceToken token = ServiceToken.Create(typeId, serviceKey);
        Type serviceType = typeof(object);
        object service = new();

        _register
            .IsDAsyncService(typeId, out Arg.Any<Type?>())
            .Returns(call =>
            {
                call[1] = serviceType;
                return true;
            });

        _provider
            .GetRequiredKeyedService(serviceType, serviceKey)
            .Returns(service);

        RootDTaskScope sut = new(_provider, _register);

        // Act
        bool result = sut.TryGetReference(token, out object? reference);

        // Assert
        result.Should().BeTrue();
        reference.Should().BeSameAs(service);
    }
}
