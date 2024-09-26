using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using static DTasks.Inspection.InspectionConstants;

namespace DTasks.Inspection;

internal sealed class ConstructorDescriptor : IConstructorDescriptor
{
    private readonly MethodInfo _handleFieldMethod;
    private readonly FrozenDictionary<Type, MethodInfo> _handleFieldSpecializedMethods;

    private ConstructorDescriptor(
        Type type,
        MethodInfo handleFieldMethod,
        MethodInfo handleAwaiterMethod,
        MethodInfo handleStateMethod,
        FrozenDictionary<Type, MethodInfo> handleFieldSpecializedMethods)
    {
        Type = type;
        _handleFieldMethod = handleFieldMethod;
        HandleAwaiterMethod = handleAwaiterMethod;
        HandleStateMethod = handleStateMethod;
        _handleFieldSpecializedMethods = handleFieldSpecializedMethods;
    }

    public Type Type { get; }

    public MethodInfo HandleStateMethod { get; }

    public MethodInfo HandleAwaiterMethod { get; }

    public MethodInfo GetHandleFieldMethod(Type fieldType)
    {
        if (_handleFieldSpecializedMethods.TryGetValue(fieldType, out MethodInfo? method))
            return method;

        return _handleFieldMethod.MakeGenericMethod(fieldType);
    }

    public static bool TryCreate(Type deconstructorType, [NotNullWhen(true)] out ConstructorDescriptor? descriptor)
    {
        MethodInfo? handleFieldMethod = deconstructorType.GetMethod(
            name: MethodNames.HandleField,
            genericParameterCount: 1,
            types: [typeof(string), Type.MakeGenericMethodParameter(0).MakeByRefType()]);

        if (handleFieldMethod is null || handleFieldMethod.ReturnType != typeof(bool))
            return False(out descriptor);

        MethodInfo? handleAwaiterMethod = deconstructorType.GetMethod(
            name: MethodNames.HandleAwaiter,
            genericParameterCount: 0,
            types: [typeof(string)]);

        if (handleAwaiterMethod is null || handleAwaiterMethod.ReturnType != typeof(bool))
            return False(out descriptor);

        Dictionary<Type, MethodInfo> handleFieldSpecializedMethods = [];

        foreach (MethodInfo method in deconstructorType.GetMethods())
        {
            if (method.Name != MethodNames.HandleField)
                continue;

            if (method == handleFieldMethod)
                continue;

            if (method.ReturnType != typeof(bool))
                continue;

            if (method.GetParameters() is not [ParameterInfo firstParam, ParameterInfo secondParam])
                continue;

            if (firstParam.ParameterType != typeof(string))
                continue;

            if (!secondParam.ParameterType.IsByRef)
                continue;

            handleFieldSpecializedMethods.Add(secondParam.ParameterType.GetElementType()!, method);
        }

        MethodInfo? handleStateMethod = deconstructorType.GetMethod(
            name: MethodNames.HandleState,
            genericParameterCount: 0,
            types: [typeof(string), typeof(int).MakeByRefType()]);

        if (handleStateMethod is not null && handleStateMethod.ReturnType != typeof(bool))
        {
            handleStateMethod = null;
        }

        handleStateMethod ??= handleFieldSpecializedMethods.TryGetValue(typeof(int), out MethodInfo? onIntField)
            ? onIntField
            : handleFieldMethod.MakeGenericMethod(typeof(int));

        descriptor = new ConstructorDescriptor(deconstructorType, handleFieldMethod, handleAwaiterMethod, handleStateMethod, handleFieldSpecializedMethods.ToFrozenDictionary());
        return true;

        static bool False(out ConstructorDescriptor? descriptor)
        {
            descriptor = null;
            return false;
        }
    }
}
