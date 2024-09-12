using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DTasks.Extensions.Microsoft.DependencyInjection;

internal static class ServiceContainerBuilderExtensions
{
    public static void ScanAndIntercept(this IServiceContainerBuilder containerBuilder, IServiceCollection services)
    {
        // TODO: Support generic types
        HashSet<Type> candidateServiceTypes = services
            .Select(descriptor => descriptor.ServiceType)
            .SelectMany(serviceType => serviceType.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            .Where(method => method.IsDefined(typeof(AsyncStateMachineAttribute)) && typeof(DTask).IsAssignableFrom(method.ReturnType))
            .SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType).Prepend(method.DeclaringType!))
            .Where(type => !type.IsValueType && !type.ContainsGenericParameters)
            .ToHashSet();

        List<ServiceDescriptor> toReplace = services
            .Where(descriptor => candidateServiceTypes.Contains(descriptor.ServiceType))
            .ToList();

        foreach (ServiceDescriptor descriptor in toReplace)
        {
            containerBuilder.Intercept(descriptor);
        }
    }
}
