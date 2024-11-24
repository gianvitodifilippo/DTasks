using System.Reflection;

namespace DTasks.Utils;

internal static class ReflectionExtensions
{
    public static MethodInfo GetRequiredMethod(this Type type, string name, BindingFlags bindingAttr, Type[] parameterTypes)
    {
        return type.GetMethod(name, bindingAttr, null, parameterTypes, null) ?? throw new MissingMethodException(type.Name, name);
    }

    public static ConstructorInfo GetRequiredConstructor(this Type type, BindingFlags bindingAttr, Type[] parameterTypes)
    {
        return type.GetConstructor(bindingAttr, null, parameterTypes, null) ?? throw new MissingMethodException(type.Name, ConstructorInfo.ConstructorName);
    }

    public static FieldInfo GetRequiredField(this Type type, string name, BindingFlags bindingAttr)
    {
        return type.GetField(name, bindingAttr) ?? throw new MissingFieldException(type.Name, name);
    }
}
