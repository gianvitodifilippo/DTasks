using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DTasks.Inspection.Dynamic.Descriptors;

internal sealed class ReaderDescriptor(
    Type type,
    MethodInfo readFieldGenericMethod,
    Dictionary<Type, MethodInfo> readFieldSpecializedMethods) : IReaderDescriptor
{
    public Type Type => type;

    public MethodInfo GetReadFieldMethod(Type fieldType) => readFieldSpecializedMethods.TryGetValue(fieldType, out MethodInfo? readFieldMethod)
        ? readFieldMethod
        : readFieldGenericMethod.MakeGenericMethod(fieldType);

    public static bool TryCreate(Type readerType, [NotNullWhen(true)] out ReaderDescriptor? descriptor)
    {
        MethodInfo? readFieldGenericMethod = null;
        Dictionary<Type, MethodInfo> readFieldSpecializedMethods = [];

        MethodInfo[] methods = readerType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
        foreach (MethodInfo method in methods)
        {
            if (method.Name != InspectionConstants.ReadFieldMethodName)
                continue;

            ParameterInfo[] parameters = method.GetParameters();
            if (method.IsGenericMethodDefinition)
            {
                if (!TrySetReadFieldGenericMethod(method, parameters))
                    continue;
            }
            else
            {
                if (!TrySetReadFieldSpecializedMethod(method, parameters))
                    continue;
            }
        }

        if (readFieldGenericMethod is null)
            return False(out descriptor);

        descriptor = new(readerType, readFieldGenericMethod, readFieldSpecializedMethods);
        return true;

        bool TrySetReadFieldGenericMethod(MethodInfo method, ParameterInfo[] parameters)
        {
            if (method.GetGenericArguments() is not [Type fieldType])
                return false;

            if (parameters is not [ParameterInfo fieldNameParam, ParameterInfo fieldParam])
                return false;

            if (fieldNameParam.ParameterType != typeof(string))
                return false;

            if (!fieldParam.ParameterType.IsByRef || fieldParam.ParameterType.GetElementType() != fieldType)
                return false;

            if (method.ReturnType != typeof(bool))
                return false;

            if (readFieldGenericMethod is not null)
                return false;

            readFieldGenericMethod = method;
            return true;
        }

        bool TrySetReadFieldSpecializedMethod(MethodInfo method, ParameterInfo[] parameters)
        {
            if (parameters is not [ParameterInfo fieldNameParam, ParameterInfo fieldParam])
                return false;

            if (fieldNameParam.ParameterType != typeof(string))
                return false;

            if (!fieldParam.ParameterType.IsByRef)
                return false;

            if (method.ReturnType != typeof(bool))
                return false;

            Type fieldType = fieldParam.ParameterType.GetElementType()!;
            if (readFieldSpecializedMethods.ContainsKey(fieldType))
                return false;

            readFieldSpecializedMethods.Add(fieldType, method);
            return true;
        }

        static bool False(out ReaderDescriptor? descriptor)
        {
            descriptor = null;
            return false;
        }
    }
}
