using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DTasks.Inspection.Dynamic.Descriptors;

internal sealed class WriterDescriptor(
    Type type,
    MethodInfo writeFieldGenericMethod,
    Dictionary<Type, MethodInfo> writeFieldSpecializedMethods) : IWriterDescriptor
{
    public Type Type => type;

    public MethodInfo GetWriteFieldMethod(Type fieldType) => writeFieldSpecializedMethods.TryGetValue(fieldType, out MethodInfo? writeFieldMethod)
        ? writeFieldMethod
        : writeFieldGenericMethod.MakeGenericMethod(fieldType);

    public static bool TryCreate(Type writerType, [NotNullWhen(true)] out WriterDescriptor? descriptor)
    {
        MethodInfo? writeFieldGenericMethod = null;
        Dictionary<Type, MethodInfo> writeFieldSpecializedMethods = [];

        MethodInfo[] methods = writerType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
        foreach (MethodInfo method in methods)
        {
            if (method.Name != InspectionConstants.WriteFieldMethodName)
                continue;

            ParameterInfo[] parameters = method.GetParameters();

            if (method.IsGenericMethodDefinition)
            {
                if (!TrySetWriteFieldGenericMethod(method, parameters))
                    continue;
            }
            else
            {
                if (!TrySetWriteFieldSpecializedMethod(method, parameters))
                    continue;
            }
        }

        if (writeFieldGenericMethod is null)
            return False(out descriptor);

        descriptor = new(writerType, writeFieldGenericMethod, writeFieldSpecializedMethods);
        return true;

        bool TrySetWriteFieldGenericMethod(MethodInfo method, ParameterInfo[] parameters)
        {
            if (method.GetGenericArguments() is not [Type fieldType])
                return false;

            if (parameters is not [ParameterInfo fieldNameParam, ParameterInfo fieldParam])
                return false;

            if (fieldNameParam.ParameterType != typeof(string))
                return false;

            if (fieldParam.ParameterType != fieldType)
                return false;

            if (method.ReturnType != typeof(void))
                return false;

            if (writeFieldGenericMethod is not null)
                return false;

            writeFieldGenericMethod = method;
            return true;
        }

        bool TrySetWriteFieldSpecializedMethod(MethodInfo method, ParameterInfo[] parameters)
        {
            if (parameters is not [ParameterInfo fieldNameParam, ParameterInfo fieldParam])
                return false;

            if (fieldNameParam.ParameterType != typeof(string))
                return false;

            if (method.ReturnType != typeof(void))
                return false;

            if (writeFieldSpecializedMethods.ContainsKey(fieldParam.ParameterType))
                return false;

            writeFieldSpecializedMethods.Add(fieldParam.ParameterType, method);
            return true;
        }

        static bool False(out WriterDescriptor? descriptor)
        {
            descriptor = null;
            return false;
        }
    }
}
