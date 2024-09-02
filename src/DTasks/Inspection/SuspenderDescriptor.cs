using DTasks.Hosting;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DTasks.Inspection;

internal sealed class SuspenderDescriptor : ISuspenderDescriptor
{
    private SuspenderDescriptor(Type delegateType, ParameterInfo deconstructorParameter, IDeconstructorDescriptor deconstructorDescriptor)
    {
        DelegateType = delegateType;
        DeconstructorParameter = deconstructorParameter;
        DeconstructorDescriptor = deconstructorDescriptor;
    }

    public Type DelegateType { get; }

    public ParameterInfo DeconstructorParameter { get; }

    public IDeconstructorDescriptor DeconstructorDescriptor { get; }

    public static bool TryCreate(Type suspenderType, [NotNullWhen(true)] out SuspenderDescriptor? descriptor)
    {
        if (!typeof(Delegate).IsAssignableFrom(suspenderType) || !suspenderType.IsGenericTypeDefinition)
            return False(out descriptor);

        if (suspenderType.GetGenericArguments() is not [Type stateMachineType])
            return False(out descriptor);

        if (stateMachineType.GetGenericParameterConstraints() is not [])
            return False(out descriptor);

        MethodInfo? invokeMethod = suspenderType.GetMethod("Invoke");
        Debug.Assert(invokeMethod is not null);

        if (invokeMethod.ReturnType != typeof(void))
            return False(out descriptor);

        if (invokeMethod.GetParameters() is not [ParameterInfo stateMachineParameter, ParameterInfo suspensionInfoParameter, ParameterInfo deconstructorParameter])
            return False(out descriptor);

        if (!stateMachineParameter.ParameterType.IsByRef || stateMachineParameter.ParameterType.GetElementType() != stateMachineType)
            return False(out descriptor);

        if (suspensionInfoParameter.ParameterType != typeof(IStateMachineInfo))
            return False(out descriptor);

        Type deconstructorType = deconstructorParameter.ParameterType;
        if (deconstructorType.IsByRef)
        {
            deconstructorType = deconstructorType.GetElementType();
        }

        if (!Inspection.DeconstructorDescriptor.TryCreate(deconstructorType, out DeconstructorDescriptor? deconstructorDescriptor))
            return False(out descriptor);

        descriptor = new SuspenderDescriptor(suspenderType, deconstructorParameter, deconstructorDescriptor);
        return true;

        static bool False(out SuspenderDescriptor? descriptor)
        {
            descriptor = default;
            return false;
        }
    }
}