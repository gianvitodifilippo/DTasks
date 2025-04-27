using DTasks.Configuration;
using DTasks.Configuration.DependencyInjection;
using DTasks.Extensions.DependencyInjection.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.DependencyInjection;

public partial class DTasksServiceCollectionExtensionsTests
{
    private readonly IServiceCollection _services;

    public DTasksServiceCollectionExtensionsTests()
    {
        _services = new ServiceCollection();
    }

    [Fact]
    public void AddDTasks_WithConfiguration_AddsConsumerServices()
    {
        // Arrange

        // Act
        _services.AddDTasks(config => config
            .ConfigureState(state => state
                .UseStack(ComponentDescriptor.Singleton(Substitute.For<IDAsyncStack>()))
                .UseHeap(ComponentDescriptor.Singleton(Substitute.For<IDAsyncHeap>()))));

        // Assert
        _services.Should().ContainSingle(Singleton<DTasksConfiguration>());
        _services.Should().ContainSingle(Singleton<DAsyncServiceValidator>());
        _services.Should().ContainSingle(Singleton<IDAsyncServiceRegister>());
        _services.Should().ContainSingle(Singleton<IServiceMapper>());
        _services.Should().ContainSingle(Singleton<DAsyncSurrogatorProvider>());
        _services.Should().ContainSingle(Singleton<IRootDAsyncSurrogator>());
        _services.Should().ContainSingle(Singleton<IRootServiceMapper>());
        _services.Should().ContainSingle(Singleton<RootDAsyncSurrogator>());
        _services.Should().ContainSingle(Scoped<IChildServiceMapper>());
        _services.Should().ContainSingle(Scoped<ChildDAsyncSurrogator>());
    }

    [Fact]
    public void AddDTasks_CannotBeCalledTwice()
    {
        // Arrange
        _services.AddDTasks(config => config
            .ConfigureState(state => state
                .UseStack(ComponentDescriptor.Singleton(Substitute.For<IDAsyncStack>()))
                .UseHeap(ComponentDescriptor.Singleton(Substitute.For<IDAsyncHeap>()))));

        // Act
        Action act = () => _services.AddDTasks(config => { });

        // Assert
        act.Should().ThrowExactly<InvalidOperationException>();
    }
}

