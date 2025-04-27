using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        _hostBuilder.UseDTasks(options => { });

        // Assert
        _hostBuilder.Received().UseServiceProviderFactory(Arg.Is(ReturningDTasksServiceProviderFactory()));
    }

    private static Expression<Predicate<Func<HostBuilderContext, IServiceProviderFactory<IServiceCollection>>>> ReturningDTasksServiceProviderFactory()
    {
        return func => func(null!) is DTasksServiceProviderFactory;
    }
}
