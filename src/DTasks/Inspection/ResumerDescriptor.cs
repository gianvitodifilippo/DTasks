using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using static DTasks.Inspection.InspectionConstants;

namespace DTasks.Inspection;

internal sealed class ResumerDescriptor : IResumerDescriptor
{
    private ResumerDescriptor(Type delegateType, ParameterInfo constructorParameter, IConstructorDescriptor constructorDescriptor)
    {
        DelegateType = delegateType;
        ConstructorParameter = constructorParameter;
        ConstructorDescriptor = constructorDescriptor;
    }

    public Type DelegateType { get; }

    public ParameterInfo ConstructorParameter { get; }

    public IConstructorDescriptor ConstructorDescriptor { get; }

    public static bool TryCreate(Type suspenderType, [NotNullWhen(true)] out ResumerDescriptor? descriptor)
    {
        if (!typeof(Delegate).IsAssignableFrom(suspenderType) || suspenderType.IsGenericTypeDefinition)
            return False(out descriptor);

        MethodInfo? invokeMethod = suspenderType.GetMethod(MethodNames.Invoke);
        Debug.Assert(invokeMethod is not null, "A delegate should have the 'Invoke' method.");

        if (invokeMethod.ReturnType != typeof(DTask))
            return False(out descriptor);

        if (invokeMethod.GetParameters() is not [ParameterInfo resultTaskParameter, ParameterInfo constructorParameter])
            return False(out descriptor);

        if (resultTaskParameter.ParameterType != typeof(DTask))
            return False(out descriptor);

        Type constructorType = constructorParameter.ParameterType;
        if (constructorType.IsByRef)
        {
            constructorType = constructorType.GetElementType();
        }

        if (!Inspection.ConstructorDescriptor.TryCreate(constructorType, out ConstructorDescriptor? constructorDescriptor))
            return False(out descriptor);

        descriptor = new ResumerDescriptor(suspenderType, constructorParameter, constructorDescriptor);
        return true;

        static bool False(out ResumerDescriptor? descriptor)
        {
            descriptor = default;
            return false;
        }
    }
}
