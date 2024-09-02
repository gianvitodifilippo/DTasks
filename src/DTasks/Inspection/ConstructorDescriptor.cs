using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DTasks.Inspection;

internal sealed class ConstructorDescriptor : IConstructorDescriptor
{
    private readonly MethodInfo _onFieldMethod;
    private readonly FrozenDictionary<Type, MethodInfo> _onFieldSpecializedMethods;

    private ConstructorDescriptor(
        Type type,
        MethodInfo onFieldMethod,
        MethodInfo onAwaiterMethod,
        MethodInfo onStateMethod,
        FrozenDictionary<Type, MethodInfo> onFieldSpecializedMethods)
    {
        Type = type;
        _onFieldMethod = onFieldMethod;
        OnAwaiterMethod = onAwaiterMethod;
        OnStateMethod = onStateMethod;
        _onFieldSpecializedMethods = onFieldSpecializedMethods;
    }

    public Type Type { get; }

    public MethodInfo OnStateMethod { get; }

    public MethodInfo OnAwaiterMethod { get; }

    public MethodInfo GetOnFieldMethod(Type fieldType)
    {
        if (_onFieldSpecializedMethods.TryGetValue(fieldType, out MethodInfo? method))
            return method;

        return _onFieldMethod.MakeGenericMethod(fieldType);
    }

    public static bool TryCreate(Type deconstructorType, [NotNullWhen(true)] out ConstructorDescriptor? descriptor)
    {
        MethodInfo? onFieldMethod = deconstructorType.GetMethod(
            name: "OnField",
            genericParameterCount: 1,
            types: [typeof(string), Type.MakeGenericMethodParameter(0).MakeByRefType()]);

        if (onFieldMethod is null || onFieldMethod.ReturnType != typeof(bool))
            return False(out descriptor);

        MethodInfo? onAwaiterMethod = deconstructorType.GetMethod(
            name: "OnAwaiter",
            genericParameterCount: 0,
            types: [typeof(string)]);

        if (onAwaiterMethod is null || onAwaiterMethod.ReturnType != typeof(bool))
            return False(out descriptor);

        Dictionary<Type, MethodInfo> onFieldSpecializedMethods = [];

        foreach (MethodInfo method in deconstructorType.GetMethods())
        {
            if (method.Name != "OnField")
                continue;

            if (method == onFieldMethod)
                continue;

            if (method.ReturnType != typeof(bool))
                continue;

            if (method.GetParameters() is not [ParameterInfo firstParam, ParameterInfo secondParam])
                continue;

            if (firstParam.ParameterType != typeof(string))
                continue;

            if (!secondParam.ParameterType.IsByRef)
                continue;

            onFieldSpecializedMethods.Add(secondParam.ParameterType.GetElementType(), method);
        }

        MethodInfo? onStateMethod = deconstructorType.GetMethod(
            name: "OnState",
            genericParameterCount: 0,
            types: [typeof(string), typeof(int).MakeByRefType()]);

        if (onStateMethod is not null && onStateMethod.ReturnType != typeof(bool))
        {
            onStateMethod = null;
        }

        onStateMethod ??= onFieldSpecializedMethods.TryGetValue(typeof(int), out MethodInfo onIntField)
            ? onIntField
            : onFieldMethod.MakeGenericMethod(typeof(int));

        descriptor = new ConstructorDescriptor(deconstructorType, onFieldMethod, onAwaiterMethod, onStateMethod, onFieldSpecializedMethods.ToFrozenDictionary());
        return true;

        static bool False(out ConstructorDescriptor? descriptor)
        {
            descriptor = null;
            return false;
        }
    }
}
