using DTasks.Infrastructure.Marshaling;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.DependencyInjection.Infrastructure.Marshaling;

public class ServiceProviderDAsyncSurrogatorTests
{
    private static readonly TypeId s_surrogateTypeId = TypeId.FromConstant("surrogate");
    private static readonly TypeId s_serviceTypeId = TypeId.FromConstant("service");

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
        var marshaller = Substitute.For<IMarshaller>();

        RootDAsyncSurrogator sut = new(_provider, _register, _typeResolver);

        // Act
        bool result = sut.TrySurrogate(in service, ref marshaller);

        // Assert
        result.Should().BeFalse();
        marshaller.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public void TrySurrogate_ReturnsTrue_WhenScopeIsRootAndServiceWasMapped()
    {
        // Arrange
        Service service = new();
        ServiceSurrogate surrogate = new();
        var marshaller = Substitute.For<IMarshaller>();

        RootDAsyncSurrogator sut = new(_provider, _register, _typeResolver);

        sut.MapService(service, surrogate);

        // Act
        bool result = sut.TrySurrogate(in service, ref marshaller);

        // Assert
        result.Should().BeTrue();
        marshaller.Received().WriteSurrogate(s_surrogateTypeId, surrogate);
    }

    [Fact]
    public void TrySurrogate_ReturnsFalse_WhenScopeIsChildAndServiceWasNotMapped()
    {
        // Arrange
        Service service = new();
        var marshaller = Substitute.For<IMarshaller>();

        RootDAsyncSurrogator root = new(_provider, _register, _typeResolver);
        ChildDAsyncSurrogator sut = new(_provider, _register, _typeResolver, root);

        // Act
        bool result = sut.TrySurrogate(in service, ref marshaller);

        // Assert
        result.Should().BeFalse();
        marshaller.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public void TrySurrogate_ReturnsTrue_WhenScopeIsChildAndServiceWasMappedInRoot()
    {
        // Arrange
        Service service = new();
        ServiceSurrogate surrogate = new();
        var marshaller = Substitute.For<IMarshaller>();

        RootDAsyncSurrogator root = new(_provider, _register, _typeResolver);
        ChildDAsyncSurrogator sut = new(_provider, _register, _typeResolver, root);

        root.MapService(service, surrogate);

        // Act
        bool result = sut.TrySurrogate(in service, ref marshaller);

        // Assert
        result.Should().BeTrue();
        marshaller.Received().WriteSurrogate(s_surrogateTypeId, surrogate);
    }

    [Fact]
    public void TrySurrogate_ReturnsTrue_WhenScopeIsChildAndServiceWasMappedInChild()
    {
        // Arrange
        Service service = new();
        ServiceSurrogate surrogate = new();
        var marshaller = Substitute.For<IMarshaller>();

        RootDAsyncSurrogator root = new(_provider, _register, _typeResolver);
        ChildDAsyncSurrogator sut = new(_provider, _register, _typeResolver, root);

        sut.MapService(service, surrogate);

        // Act
        bool result = sut.TrySurrogate(in service, ref marshaller);

        // Assert
        result.Should().BeTrue();
        marshaller.Received().WriteSurrogate(s_surrogateTypeId, surrogate);
    }

    [Fact]
    public void TryRestore_ReturnsFalse_WhenTokenIsOfTheWrongType()
    {
        // Arrange
        var unmarshaller = Substitute.For<IUnmarshaller>();

        RootDAsyncSurrogator sut = new(_provider, _register, _typeResolver);

        // Act
        bool result = sut.TryRestore(default, ref unmarshaller, out Service? service);

        // Assert
        result.Should().BeFalse();
        unmarshaller.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public void TryRestore_ReturnsTrue_WhenTokenIsServiceToken()
    {
        // Arrange
        var unmarshaller = Substitute.For<IUnmarshaller>();
        Service expectedService = new();

        _typeResolver.GetType(s_surrogateTypeId).Returns(typeof(ServiceSurrogate));

        unmarshaller
            .ReadSurrogate<ServiceSurrogate>(typeof(ServiceSurrogate))
            .Returns(new ServiceSurrogate { TypeId = s_serviceTypeId });

        _provider
            .GetService(typeof(Service))
            .Returns(expectedService);

        _register
            .IsDAsyncService(s_serviceTypeId, out Arg.Any<Type?>())
            .Returns(call =>
            {
                call[1] = typeof(Service);
                return true;
            });

        RootDAsyncSurrogator sut = new(_provider, _register, _typeResolver);

        // Act
        bool result = sut.TryRestore(s_surrogateTypeId, ref unmarshaller, out Service? service);

        // Assert
        result.Should().BeTrue();
        service.Should().BeSameAs(expectedService);
        unmarshaller.Received().ReadSurrogate<ServiceSurrogate>(typeof(ServiceSurrogate));
    }

    [Fact]
    public void TryRestore_ReturnsTrue_WhenTokenIsKeyedServiceToken()
    {
        // Arrange
        var unmarshaller = Substitute.For<IUnmarshaller>();
        string key = "key";
        Service expectedService = new();

        _typeResolver.GetType(s_surrogateTypeId).Returns(typeof(KeyedServiceSurrogate<string>));

        unmarshaller
            .ReadSurrogate<ServiceSurrogate>(typeof(KeyedServiceSurrogate<string>))
            .Returns(new KeyedServiceSurrogate<string> { TypeId = s_serviceTypeId, Key = key });

        _provider
            .GetRequiredKeyedService(typeof(Service), key)
            .Returns(expectedService);

        _register
            .IsDAsyncService(s_serviceTypeId, out Arg.Any<Type?>())
            .Returns(call =>
            {
                call[1] = typeof(Service);
                return true;
            });

        RootDAsyncSurrogator sut = new(_provider, _register, _typeResolver);

        // Act
        bool result = sut.TryRestore(s_surrogateTypeId, ref unmarshaller, out Service? service);

        // Assert
        result.Should().BeTrue();
        service.Should().BeSameAs(expectedService);
        unmarshaller.Received().ReadSurrogate<ServiceSurrogate>(typeof(KeyedServiceSurrogate<string>));
    }

    private sealed class Service;
}
