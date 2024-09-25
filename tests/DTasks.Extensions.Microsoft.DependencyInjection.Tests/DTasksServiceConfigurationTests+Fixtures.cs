namespace DTasks.Extensions.Microsoft.DependencyInjection;

public partial class DTasksServiceConfigurationTests
{
    private interface IServiceWithDTaskMethod
    {
        DTask MyMethodDAsync();
    }

    private interface IServiceWithoutDTaskMethods
    {
        Task MyMethodAsync();
    }

    private interface IServiceWithDTaskMethod<T>
    {
        DTask MyMethodDAsync();
    }

    private interface IServiceWithoutDTaskMethods<T>
    {
        Task MyMethodAsync();
    }
}
