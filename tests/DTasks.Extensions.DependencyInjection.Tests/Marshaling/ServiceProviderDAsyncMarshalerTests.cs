using DTasks.Marshaling;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.DependencyInjection.Marshaling;

public class ServiceProviderDAsyncMarshalerTests
{
    private static readonly TypeId s_tokenTypeId = new("token");
    private static readonly TypeId s_serviceTypeId = new("service");

    private readonly IKeyedServiceProvider _provider;
    private readonly IDAsyncServiceRegister _register;
    private readonly ITypeResolver _typeResolver;

    public ServiceProviderDAsyncMarshalerTests()
    {
        _provider = Substitute.For<IKeyedServiceProvider>();
        _register = Substitute.For<IDAsyncServiceRegister>();
        _typeResolver = Substitute.For<ITypeResolver>();

        _typeResolver.GetTypeId(typeof(ServiceToken)).Returns(s_tokenTypeId);
        _typeResolver.GetTypeId(typeof(Service)).Returns(s_serviceTypeId);
        _typeResolver.GetType(s_tokenTypeId).Returns(typeof(ServiceToken));
        _typeResolver.GetType(s_serviceTypeId).Returns(typeof(Service));
    }

    [Fact]
    public void TryMarshal_ReturnsFalse_WhenScopeIsRootAndServiceWasNotMapped()
    {
        // Arrange
        Service service = new();
        IMarshalingAction action = Substitute.For<IMarshalingAction>();

        RootServiceProviderDAsyncMarshaler sut = new(_provider, _register, _typeResolver);

        // Act
        bool result = sut.TryMarshal(in service, action);

        // Assert
        result.Should().BeFalse();
        action.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public void TryMarshal_ReturnsTrue_WhenScopeIsRootAndServiceWasMapped()
    {
        // Arrange
        Service service = new();
        ServiceToken token = new();
        IMarshalingAction action = Substitute.For<IMarshalingAction>();

        RootServiceProviderDAsyncMarshaler sut = new(_provider, _register, _typeResolver);

        sut.MapService(service, token);

        // Act
        bool result = sut.TryMarshal(in service, action);

        // Assert
        result.Should().BeTrue();
        action.Received().MarshalAs(s_tokenTypeId, token);
    }

    [Fact]
    public void TryMarshal_ReturnsFalse_WhenScopeIsChildAndServiceWasNotMapped()
    {
        // Arrange
        Service service = new();
        IMarshalingAction action = Substitute.For<IMarshalingAction>();

        RootServiceProviderDAsyncMarshaler root = new(_provider, _register, _typeResolver);
        ChildServiceProviderDAsyncMarshaler sut = new(_provider, _register, _typeResolver, root);

        // Act
        bool result = sut.TryMarshal(in service, action);

        // Assert
        result.Should().BeFalse();
        action.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public void TryMarshal_ReturnsTrue_WhenScopeIsChildAndServiceWasMappedInRoot()
    {
        // Arrange
        Service service = new();
        ServiceToken token = new();
        IMarshalingAction action = Substitute.For<IMarshalingAction>();

        RootServiceProviderDAsyncMarshaler root = new(_provider, _register, _typeResolver);
        ChildServiceProviderDAsyncMarshaler sut = new(_provider, _register, _typeResolver, root);

        root.MapService(service, token);

        // Act
        bool result = sut.TryMarshal(in service, action);

        // Assert
        result.Should().BeTrue();
        action.Received().MarshalAs(s_tokenTypeId, token);
    }

    [Fact]
    public void TryMarshal_ReturnsTrue_WhenScopeIsChildAndServiceWasMappedInChild()
    {
        // Arrange
        Service service = new();
        ServiceToken token = new();
        IMarshalingAction action = Substitute.For<IMarshalingAction>();

        RootServiceProviderDAsyncMarshaler root = new(_provider, _register, _typeResolver);
        ChildServiceProviderDAsyncMarshaler sut = new(_provider, _register, _typeResolver, root);

        sut.MapService(service, token);

        // Act
        bool result = sut.TryMarshal(in service, action);

        // Assert
        result.Should().BeTrue();
        action.Received().MarshalAs(s_tokenTypeId, token);
    }

    [Fact]
    public void TryUnmarshal_ReturnsFalse_WhenTokenIsOfTheWrongType()
    {
        // Arrange
        IUnmarshalingAction action = Substitute.For<IUnmarshalingAction>();

        RootServiceProviderDAsyncMarshaler sut = new(_provider, _register, _typeResolver);

        // Act
        bool result = sut.TryUnmarshal<Service>(default, action);

        // Assert
        result.Should().BeFalse();
        action.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public void TryUnmarshal_ReturnsTrue_WhenTokenIsServiceToken()
    {
        // Arrange
        IUnmarshalingAction action = Substitute.For<IUnmarshalingAction>();

        RootServiceProviderDAsyncMarshaler sut = new(_provider, _register, _typeResolver);

        // Act
        bool result = sut.TryUnmarshal<Service>(s_tokenTypeId, action);

        // Assert
        result.Should().BeTrue();
        action.Received().UnmarshalAs(typeof(ServiceToken), Arg.Any<ITokenConverter>());
    }

    [Fact]
    public void TryUnmarshal_ReturnsTrue_WhenTokenIsKeyedServiceToken()
    {
        // Arrange
        IUnmarshalingAction action = Substitute.For<IUnmarshalingAction>();

        _typeResolver.GetTypeId(typeof(KeyedServiceToken<string>)).Returns(s_tokenTypeId);
        _typeResolver.GetType(s_tokenTypeId).Returns(typeof(KeyedServiceToken<string>));

        RootServiceProviderDAsyncMarshaler sut = new(_provider, _register, _typeResolver);

        // Act
        bool result = sut.TryUnmarshal<Service>(s_tokenTypeId, action);

        // Assert
        result.Should().BeTrue();
        action.Received().UnmarshalAs(typeof(KeyedServiceToken<string>), Arg.Any<ITokenConverter>());
    }

    private sealed class Service;
}
