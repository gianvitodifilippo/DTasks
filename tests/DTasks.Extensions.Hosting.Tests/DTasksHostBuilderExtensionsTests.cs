using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq.Expressions;

namespace DTasks.Extensions.Hosting;

public class DTasksHostBuilderExtensionsTests
{
    private readonly IHostBuilder _hostBuilder;

    public DTasksHostBuilderExtensionsTests()
    {
        _hostBuilder = Substitute.For<IHostBuilder>();
    }

    [Fact]
    public void UseDTasks_UsesDTasksServiceProviderFactory()
    {
        // Arrange

        // Act
        _hostBuilder.UseDTasks();

        // Assert
        _hostBuilder.Received().UseServiceProviderFactory(Arg.Is(ReturningDTasksServiceProviderFactory()));
    }

    //[Fact]
    //public void UseDTasks_WithOptions_UsesDTasksServiceProviderFactory()
    //{
    //    // Arrange
    //    ServiceProviderOptions options = new();

    //    // Act
    //    _hostBuilder.UseDTasks(options);

    //    // Assert
    //    _hostBuilder.Received().UseServiceProviderFactory(Arg.Is(ReturningDTasksServiceProviderFactory()));
    //}

    [Fact]
    public void UseDTasks_WithConfigureOptions_UsesDTasksServiceProviderFactory()
    {
        // Arrange

        // Act
        _hostBuilder.UseDTasks(options => { });

        // Assert
        _hostBuilder.Received().UseServiceProviderFactory(Arg.Is(ReturningDTasksServiceProviderFactory()));
    }

    //[Fact]
    //public void UseDTasks_WithConfigureOptionsWithContext_UsesDTasksServiceProviderFactory()
    //{
    //    // Arrange

    //    // Act
    //    _hostBuilder.UseDTasks((context, options) => { });

    //    // Assert
    //    _hostBuilder.Received().UseServiceProviderFactory(Arg.Is(ReturningDTasksServiceProviderFactory()));
    //}

    private static Expression<Predicate<Func<HostBuilderContext, IServiceProviderFactory<IServiceCollection>>>> ReturningDTasksServiceProviderFactory()
    {
        return func => func(null!) is DTasksServiceProviderFactory;
    }
}
