using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DTasks.Inspection;

internal sealed class DeconstructorDescriptor : IDeconstructorDescriptor
{
    private readonly MethodInfo _onFieldMethod;
    private readonly FrozenDictionary<Type, MethodInfo> _onFieldSpecializedMethods;

    private DeconstructorDescriptor(
        Type type,
        MethodInfo onFieldMethod,
        MethodInfo onAwaiterMethod,
        MethodInfo onStateMethod,
        FrozenDictionary<Type, MethodInfo> onFieldSpecializedMethods)
    {
        Type = type;
        _onFieldMethod = onFieldMethod;
        OnAwaiterMethod = onAwaiterMethod;
        HandleStateMethod = onStateMethod;
        _onFieldSpecializedMethods = onFieldSpecializedMethods;
    }

    public Type Type { get; }

    public MethodInfo HandleStateMethod { get; }

    public MethodInfo OnAwaiterMethod { get; }

    public MethodInfo GetHandleFieldMethod(Type fieldType)
    {
        if (_onFieldSpecializedMethods.TryGetValue(fieldType, out MethodInfo? method))
            return method;

        return _onFieldMethod.MakeGenericMethod(fieldType);
    }

    public static bool TryCreate(Type deconstructorType, [NotNullWhen(true)] out DeconstructorDescriptor? descriptor)
    {
        MethodInfo? onFieldMethod = deconstructorType.GetMethod(
            name: "OnField",
            genericParameterCount: 1,
            types: [typeof(string), Type.MakeGenericMethodParameter(0)]);

        if (onFieldMethod is null || onFieldMethod.ReturnType != typeof(void))
            return False(out descriptor);

        MethodInfo? onAwaiterMethod = deconstructorType.GetMethod(
            name: "OnAwaiter",
            genericParameterCount: 0,
            types: [typeof(string)]);

        if (onAwaiterMethod is null || onAwaiterMethod.ReturnType != typeof(void))
            return False(out descriptor);

        Dictionary<Type, MethodInfo> onFieldSpecializedMethods = [];

        foreach (MethodInfo method in deconstructorType.GetMethods())
        {
            if (method.Name != "OnField")
                continue;

            if (method == onFieldMethod)
                continue;

            if (method.ReturnType != typeof(void))
                continue;

            if (method.GetParameters() is not [ParameterInfo firstParam, ParameterInfo secondParam])
                continue;

            if (firstParam.ParameterType != typeof(string))
                continue;

            if (secondParam.ParameterType.IsByRef)
                continue;

            onFieldSpecializedMethods.Add(secondParam.ParameterType, method);
        }

        MethodInfo? onStateMethod = deconstructorType.GetMethod(
            name: "OnState",
            genericParameterCount: 0,
            types: [typeof(string), typeof(int)]);

        if (onStateMethod is not null && onStateMethod.ReturnType != typeof(void))
        {
            onStateMethod = null;
        }

        onStateMethod ??= onFieldSpecializedMethods.TryGetValue(typeof(int), out MethodInfo onIntField)
            ? onIntField
            : onFieldMethod.MakeGenericMethod(typeof(int));

        descriptor = new DeconstructorDescriptor(deconstructorType, onFieldMethod, onAwaiterMethod, onStateMethod, onFieldSpecializedMethods.ToFrozenDictionary());
        return true;

        static bool False(out DeconstructorDescriptor? descriptor)
        {
            descriptor = null;
            return false;
        }
    }
}
