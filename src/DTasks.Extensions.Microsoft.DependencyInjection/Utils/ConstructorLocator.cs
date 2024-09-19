using Microsoft.Extensions.DependencyInjection;
using System.Collections.Frozen;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DTasks.Extensions.Microsoft.DependencyInjection.Utils;

internal readonly struct ConstructorLocator(IServiceCollection services)
{
    private static readonly FrozenSet<Type> _builtInServiceTypes = new Type[]
    {
        typeof(IServiceProvider),
        typeof(IServiceScopeFactory),
        typeof(IServiceProviderIsService),
        typeof(IServiceProviderIsKeyedService)
    }.ToFrozenSet();

    public ConstructorInfo? GetDependencyInjectionConstructor(Type implementationType)
    {
        ConstructorInfo[] constructors = implementationType.GetConstructors();

        return constructors.Length switch
        {
            0 => null,
            1 => constructors[0],
            _ => GetBestConstructor(constructors)
        };
    }

    private ConstructorInfo? GetBestConstructor(ConstructorInfo[] constructors)
    {
        // This should be kept in sync with CallSiteFactory.CreateConstructorCallSite

        Array.Sort(constructors, (a, b) => b.GetParameters().Length.CompareTo(a.GetParameters().Length));

        ConstructorInfo? bestConstructor = null;
        HashSet<Type>? bestConstructorParameterTypes = null;

        for (int i = 0; i < constructors.Length; i++)
        {
            ParameterInfo[] parameters = constructors[i].GetParameters();
            if (ContainsUnresolvableParameters(parameters))
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

    private bool ContainsUnresolvableParameters(ParameterInfo[] parameters)
    {
        foreach (ParameterInfo parameter in parameters)
        {
            if (!IsResolvable(parameter))
                return true;
        }

        return false;
    }

    private bool IsResolvable(ParameterInfo parameter) =>
        IsService(parameter.ParameterType) ||
        IsEnumerableOfServices(parameter.ParameterType) ||
        IsBuiltInService(parameter.ParameterType) ||
        IsServiceKey(parameter.ParameterType) ||
        HasDefaultValue(parameter);

    private bool IsService(Type parameterType) => services.Any(descriptor => descriptor.ServiceType == parameterType);

    private bool IsEnumerableOfServices(Type parameterType) =>
        parameterType.IsGenericType &&
        parameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
        IsService(parameterType.GetGenericArguments()[0]);

    private static bool IsBuiltInService(Type parameterType) => _builtInServiceTypes.Contains(parameterType);

    private static bool IsServiceKey(Type parameterType) => parameterType.IsDefined(typeof(ServiceKeyAttribute), inherit: true); // Don't bother validating the parameter type since CallSiteFactory will throw an exception anyway

    private static bool HasDefaultValue(ParameterInfo parameter) => ParameterDefaultValue.TryGetDefaultValue(parameter, out _);
}
