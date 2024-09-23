using DTasks.Extensions.Microsoft.DependencyInjection.Hosting;
using DTasks.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.Microsoft.DependencyInjection;

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
        _services.Should().ContainSingle(Singleton<IRootDTaskScope>());
        _services.Should().ContainSingle(Scoped<IDTaskScope>());
        _services.Should().ContainSingle(Singleton<DAsyncServiceValidator>());
    }

    [Fact]
    public void AddDTasks_WithConfiguration_AddsConsumerServices()
    {
        // Arrange

        // Act
        _services.AddDTasks(config => { });

        // Assert
        _services.Should().ContainSingle(Singleton<IRootDTaskScope>());
        _services.Should().ContainSingle(Scoped<IDTaskScope>());
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
