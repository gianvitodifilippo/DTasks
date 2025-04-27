using System.Collections.Frozen;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.DependencyInjection.Utils;

internal readonly struct ConstructorLocator(IServiceCollection services)
{
    private static readonly FrozenSet<Type> s_builtInServiceTypes = new Type[]
    {
        typeof(IServiceProvider),
        typeof(IServiceScopeFactory),
        typeof(IServiceProviderIsService),
        typeof(IServiceProviderIsKeyedService)
    }.ToFrozenSet();

    public ConstructorInfo? GetDependencyInjectionConstructor(ServiceDescriptor descriptor, Type implementationType)
    {
        // This should be kept in sync with CallSiteFactory.CreateConstructorCallSite.
        // The only difference is that it returns null also when it detects that a service is not resolvable
        // so that we know to expect an error.

        ConstructorInfo[] constructors = implementationType.GetConstructors();
        Array.Sort(constructors, (a, b) => b.GetParameters().Length.CompareTo(a.GetParameters().Length));

        ConstructorInfo? bestConstructor = null;
        HashSet<Type>? bestConstructorParameterTypes = null;

        for (int i = 0; i < constructors.Length; i++)
        {
            ParameterInfo[] parameters = constructors[i].GetParameters();
            if (ContainsUnresolvableParameters(descriptor, parameters))
                continue;

            if (bestConstructor == null)
            {
                bestConstructor = constructors[i];
            }
            else
            {
                if (bestConstructorParameterTypes is null)
                {
                    ParameterInfo[] bestConstructorParameters = bestConstructor.GetParameters();
                    bestConstructorParameterTypes = new(bestConstructorParameters.Length);
                    foreach (ParameterInfo bestConstructorParameter in bestConstructorParameters)
                    {
                        bestConstructorParameterTypes.Add(bestConstructorParameter.ParameterType);
                    }
                }

                foreach (ParameterInfo parameter in parameters)
                {
                    if (!bestConstructorParameterTypes.Contains(parameter.ParameterType))
                        return null;
                }
            }
        }

        return bestConstructor;
    }

    private bool ContainsUnresolvableParameters(ServiceDescriptor descriptor, ParameterInfo[] parameters)
    {
        foreach (ParameterInfo parameter in parameters)
        {
            if (!IsResolvable(descriptor, parameter))
                return true;
        }

        return false;
    }

    private bool IsResolvable(ServiceDescriptor descriptor, ParameterInfo parameter) =>
        IsService(parameter.ParameterType) ||
        IsGenericService(parameter.ParameterType) ||
        IsEnumerableOfServices(parameter.ParameterType) ||
        IsBuiltInService(parameter.ParameterType) ||
        IsServiceKey(descriptor, parameter) ||
        HasDefaultValue(parameter);

    private bool IsService(Type parameterType) => services.Any(descriptor => descriptor.ServiceType == parameterType);

    private bool IsGenericService(Type parameterType) => parameterType.IsGenericType && IsService(parameterType.GetGenericTypeDefinition());

    private bool IsEnumerableOfServices(Type parameterType) =>
        parameterType.IsGenericType &&
        parameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
        IsService(parameterType.GetGenericArguments()[0]);

    private static bool IsBuiltInService(Type parameterType) => s_builtInServiceTypes.Contains(parameterType);

    private static bool IsServiceKey(ServiceDescriptor descriptor, ParameterInfo parameter) =>
        descriptor.IsKeyedService && // Instead of throwing, CallSiteFactory just ignores the [ServiceKey] attribute if the service is not keyed
        parameter.IsDefined(typeof(ServiceKeyAttribute), inherit: true);

    private static bool HasDefaultValue(ParameterInfo parameter) => ParameterDefaultValue.TryGetDefaultValue(parameter, out _);
}
