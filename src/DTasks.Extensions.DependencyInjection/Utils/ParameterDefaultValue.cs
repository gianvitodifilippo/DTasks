using System.Reflection;
using System.Runtime.CompilerServices;

namespace DTasks.Extensions.DependencyInjection.Utils;

// Taken from Microsoft.Extensions.Internal.ParameterDefaultValue
internal static class ParameterDefaultValue
{
    public static bool TryGetDefaultValue(ParameterInfo parameter, out object? defaultValue)
    {
        bool hasDefaultValue = parameter.HasDefaultValue;
        defaultValue = null;

        if (hasDefaultValue)
        {
            defaultValue = parameter.DefaultValue;

            bool isNullableParameterType =
                parameter.ParameterType.IsGenericType &&
                parameter.ParameterType.GetGenericTypeDefinition() == typeof(Nullable<>);

            if (defaultValue == null && parameter.ParameterType.IsValueType && !isNullableParameterType)
            {
                defaultValue = RuntimeHelpers.GetUninitializedObject(parameter.ParameterType);
            }

            if (defaultValue is not null && isNullableParameterType)
            {
                Type? underlyingType = Nullable.GetUnderlyingType(parameter.ParameterType);
                if (underlyingType is not null && underlyingType.IsEnum)
                {
                    defaultValue = Enum.ToObject(underlyingType, defaultValue);
                }
            }
        }

        return hasDefaultValue;
    }
}
