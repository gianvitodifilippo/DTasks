using System.Reflection;

namespace DTasks.Inspection;

internal interface IDeconstructorDescriptor
{
    Type Type { get; }

    MethodInfo HandleStateMethod { get; }

    MethodInfo OnAwaiterMethod { get; }

    MethodInfo GetHandleFieldMethod(Type fieldType);
}