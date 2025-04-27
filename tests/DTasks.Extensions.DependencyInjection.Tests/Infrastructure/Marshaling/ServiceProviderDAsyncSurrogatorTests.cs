using DTasks.Infrastructure.Marshaling;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.DependencyInjection.Infrastructure.Marshaling;

public class ServiceProviderDAsyncSurrogatorTests
{
    private static readonly TypeId s_surrogateTypeId = new("surrogate");
    private static readonly TypeId s_serviceTypeId = new("service");

    private readonly IKeyedServiceProvider _provider;
    private readonly IDAsyncServiceRegister _register;
    private readonly IDAsyncTypeResolver _typeResolver;

    public ServiceProviderDAsyncSurrogatorTests()
    {
        _provider = Substitute.For<IKeyedServiceProvider>();
        _register = Substitute.For<IDAsyncServiceRegister>();
        _typeResolver = Substitute.For<IDAsyncTypeResolver>();

        _typeResolver.GetTypeId(typeof(ServiceSurrogate)).Returns(s_surrogateTypeId);
        _typeResolver.GetTypeId(typeof(Service)).Returns(s_serviceTypeId);
        _typeResolver.GetType(s_surrogateTypeId).Returns(typeof(ServiceSurrogate));
        _typeResolver.GetType(s_serviceTypeId).Returns(typeof(Service));
    }

    [Fact]
    public void TrySurrogate_ReturnsFalse_WhenScopeIsRootAndServiceWasNotMapped()
    {
        // Arrange
        Service service = new();
        ISurrogationAction action = Substitute.For<ISurrogationAction>();

        RootDAsyncSurrogator sut = new(_provider, _register, _typeResolver);

        // Act
        bool result = sut.TrySurrogate(in service, action);

        // Assert
        result.Should().BeFalse();
        action.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public void TrySurrogate_ReturnsTrue_WhenScopeIsRootAndServiceWasMapped()
    {
        // Arrange
        Service service = new();
        ServiceSurrogate surrogate = new();
        ISurrogationAction action = Substitute.For<ISurrogationAction>();

        RootDAsyncSurrogator sut = new(_provider, _register, _typeResolver);

        sut.MapService(service, surrogate);

        // Act
        bool result = sut.TrySurrogate(in service, action);

        // Assert
        result.Should().BeTrue();
        action.Received().SurrogateAs(s_surrogateTypeId, surrogate);
    }

    [Fact]
    public void TrySurrogate_ReturnsFalse_WhenScopeIsChildAndServiceWasNotMapped()
    {
        // Arrange
        Service service = new();
        ISurrogationAction action = Substitute.For<ISurrogationAction>();

        RootDAsyncSurrogator root = new(_provider, _register, _typeResolver);
        ChildDAsyncSurrogator sut = new(_provider, _register, _typeResolver, root);

        // Act
        bool result = sut.TrySurrogate(in service, action);

        // Assert
        result.Should().BeFalse();
        action.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public void TrySurrogate_ReturnsTrue_WhenScopeIsChildAndServiceWasMappedInRoot()
    {
        // Arrange
        Service service = new();
        ServiceSurrogate surrogate = new();
        ISurrogationAction action = Substitute.For<ISurrogationAction>();

        RootDAsyncSurrogator root = new(_provider, _register, _typeResolver);
        ChildDAsyncSurrogator sut = new(_provider, _register, _typeResolver, root);

        root.MapService(service, surrogate);

        // Act
        bool result = sut.TrySurrogate(in service, action);

        // Assert
        result.Should().BeTrue();
        action.Received().SurrogateAs(s_surrogateTypeId, surrogate);
    }

    [Fact]
    public void TrySurrogate_ReturnsTrue_WhenScopeIsChildAndServiceWasMappedInChild()
    {
        // Arrange
        Service service = new();
        ServiceSurrogate surrogate = new();
        ISurrogationAction action = Substitute.For<ISurrogationAction>();

        RootDAsyncSurrogator root = new(_provider, _register, _typeResolver);
        ChildDAsyncSurrogator sut = new(_provider, _register, _typeResolver, root);

        sut.MapService(service, surrogate);

        // Act
        bool result = sut.TrySurrogate(in service, action);

        // Assert
        result.Should().BeTrue();
        action.Received().SurrogateAs(s_surrogateTypeId, surrogate);
    }

    [Fact]
    public void TryRestore_ReturnsFalse_WhenTokenIsOfTheWrongType()
    {
        // Arrange
        IRestorationAction action = Substitute.For<IRestorationAction>();

        RootDAsyncSurrogator sut = new(_provider, _register, _typeResolver);

        // Act
        bool result = sut.TryRestore<Service>(default, action);

        // Assert
        result.Should().BeFalse();
        action.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public void TryRestore_ReturnsTrue_WhenTokenIsServiceToken()
    {
        // Arrange
        IRestorationAction action = Substitute.For<IRestorationAction>();

        RootDAsyncSurrogator sut = new(_provider, _register, _typeResolver);

        // Act
        bool result = sut.TryRestore<Service>(s_surrogateTypeId, action);

        // Assert
        result.Should().BeTrue();
        action.Received().RestoreAs(typeof(ServiceSurrogate), Arg.Any<ISurrogateConverter>());
    }

    [Fact]
    public void TryRestore_ReturnsTrue_WhenTokenIsKeyedServiceToken()
    {
        // Arrange
        IRestorationAction action = Substitute.For<IRestorationAction>();

        _typeResolver.GetTypeId(typeof(KeyedServiceSurrogate<string>)).Returns(s_surrogateTypeId);
        _typeResolver.GetType(s_surrogateTypeId).Returns(typeof(KeyedServiceSurrogate<string>));

        RootDAsyncSurrogator sut = new(_provider, _register, _typeResolver);

        // Act
        bool result = sut.TryRestore<Service>(s_surrogateTypeId, action);

        // Assert
        result.Should().BeTrue();
        action.Received().RestoreAs(typeof(KeyedServiceSurrogate<string>), Arg.Any<ISurrogateConverter>());
    }

    private sealed class Service;
}
