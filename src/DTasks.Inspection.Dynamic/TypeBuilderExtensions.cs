using DTasks.Utils;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace DTasks.Inspection.Dynamic;

internal static class TypeBuilderExtensions
{
    public static MethodBuilder DefineMethodOverride(this TypeBuilder converterType, MethodInfo declaration)
    {
        ParameterInfo[] parameters = declaration.GetParameters();

        MethodBuilder method = converterType.DefineMethod(
            name: declaration.Name,
            attributes: MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final,
            callingConvention: CallingConventions.HasThis,
            returnType: declaration.ReturnType,
            returnTypeRequiredCustomModifiers: declaration.ReturnParameter.GetRequiredCustomModifiers(),
            returnTypeOptionalCustomModifiers: declaration.ReturnParameter.GetOptionalCustomModifiers(),
            parameterTypes: parameters.Map(parameter => parameter.ParameterType),
            parameterTypeRequiredCustomModifiers: parameters.Map(parameter => parameter.GetRequiredCustomModifiers()),
            parameterTypeOptionalCustomModifiers: parameters.Map(parameter => parameter.GetOptionalCustomModifiers()));

        if (declaration.IsGenericMethodDefinition)
        {
            Type[] genericParameterTypes = declaration.GetGenericArguments();
            GenericTypeParameterBuilder[] genericTypeParameters = method.DefineGenericParameters(genericParameterTypes.Map(type => type.Name));

            Debug.Assert(genericParameterTypes.Length == genericTypeParameters.Length);
            for (int i = 0; i < genericParameterTypes.Length; i++)
            {
                Type genericParameterType = genericParameterTypes[i];
                var genericTypeParameter = genericTypeParameters[i];

                if (genericParameterType.BaseType is Type baseType && baseType != typeof(object))
                {
                    genericTypeParameter.SetBaseTypeConstraint(baseType);
                }

                genericTypeParameter.SetGenericParameterAttributes(genericParameterType.GenericParameterAttributes);

                if (genericParameterType.GetInterfaces() is { Length: > 0 } interfaceTypes)
                {
                    genericTypeParameter.SetInterfaceConstraints(interfaceTypes);
                }
            }
        }

        foreach (ParameterInfo parameter in parameters)
        {
            method.DefineParameter(
                position: parameter.Position + 1,
                attributes: parameter.Attributes,
                strParamName: parameter.Name);
        }

        converterType.DefineMethodOverride(method, declaration);

        return method;
    }
}
