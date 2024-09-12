using DTasks;
using DTasks.Extensions.Microsoft.DependencyInjection;
using DTasks.Extensions.Microsoft.DependencyInjection.Hosting;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection;

public static class DTasksServiceCollectionExtensions
{
    public static IServiceCollection AddDTasks(this IServiceCollection services)
    {
        if (services.Any(descriptor => descriptor.ServiceType == typeof(DTaskScope)))
            throw new InvalidOperationException("DTasks services have already been added.");

        ServiceContainerBuilder containerBuilder = new(services);

        // TODO: Support generic types
        HashSet<Type> candidateServiceTypes = services
            .Select(descriptor => descriptor.ServiceType)
            .SelectMany(serviceType => serviceType.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            .Where(method => typeof(DTask).IsAssignableFrom(method.ReturnType))
            .SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType).Prepend(method.DeclaringType!))
            .Where(type => !type.IsValueType && !type.ContainsGenericParameters)
            .ToHashSet();

        List<ServiceDescriptor> toReplace = services
            .Where(descriptor => candidateServiceTypes.Contains(descriptor.ServiceType))
            .ToList();

        foreach (ServiceDescriptor descriptor in toReplace)
        {
            containerBuilder.Mark(descriptor);
        }

        containerBuilder.AddDTaskServices();

        return services;
    }
}
