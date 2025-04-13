using DTasks.Extensions.DependencyInjection.Marshaling;
using DTasks.Infrastructure.Marshaling;
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
    public void AddDTasks_AddsConsumerServices()
    {
        // Arrange

        // Act
        _services.AddDTasks();

        // Assert
        _services.Should().ContainSingle(Singleton<IRootDAsyncSurrogator>());
        _services.Should().ContainSingle(Scoped<IDAsyncSurrogator>());
        _services.Should().ContainSingle(Singleton<DAsyncServiceValidator>());
    }

    [Fact]
    public void AddDTasks_WithConfiguration_AddsConsumerServices()
    {
        // Arrange

        // Act
        _services.AddDTasks(config => { });

        // Assert
        _services.Should().ContainSingle(Singleton<IRootDAsyncSurrogator>());
        _services.Should().ContainSingle(Scoped<IDAsyncSurrogator>());
        _services.Should().ContainSingle(Singleton<DAsyncServiceValidator>());
    }

    [Fact]
    public void AddDTasks_CannotBeCalledTwice()
    {
        // Arrange
        _services.AddDTasks();

        // Act
        Action act = () => _services.AddDTasks();

        // Assert
        act.Should().ThrowExactly<InvalidOperationException>();
    }
}

