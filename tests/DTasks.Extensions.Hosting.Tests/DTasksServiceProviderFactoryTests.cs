using DTasks.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.Hosting;

public class DTasksServiceProviderFactoryTests
{
    [Fact]
    public void CreateBuilder_ReturnsServices()
    {
        // Arrange
        IServiceCollection services = Substitute.For<IServiceCollection>();
        DTasksServiceProviderFactory sut = new(new ServiceProviderOptions());

        // Act
        IServiceCollection builder = sut.CreateBuilder(services);

        // Assert
        builder.Should().BeSameAs(services);
    }

    [Fact]
    public void CreateServiceProvider_ReturnsServiceProvider()
    {
        // Arrange
        IServiceCollection services = Substitute.For<IServiceCollection>();
        DTasksServiceProviderFactory sut = new(new ServiceProviderOptions());

        // Act
        IServiceProvider provider = sut.CreateServiceProvider(services);

        // Assert
        provider.Should().BeOfType<ServiceProvider>();
    }

    [Fact]
    public void CreateServiceProvider_CallsValidator_WhenValidateOnBuildIsTrue()
    {
        // Arrange
        IServiceCollection services = Substitute.For<IServiceCollection>();
        DTasksServiceProviderFactory sut = new(new ServiceProviderOptions { ValidateOnBuild = true });
        DAsyncServiceValidator validator = Substitute.For<DAsyncServiceValidator>();

        services.Count.Returns(1);

        services
            .When(s => s.CopyTo(Arg.Any<ServiceDescriptor[]>(), 0))
            .Do(call => call.Arg<ServiceDescriptor[]>()[0] = ServiceDescriptor.Singleton(validator));

        // Act
        IServiceProvider provider = sut.CreateServiceProvider(services);

        // Assert
        validator.Received().Invoke();
    }
}
