using System.Reflection;

namespace DTasks.Inspection;

internal interface IConstructorDescriptor
{
    Type Type { get; }

    MethodInfo HandleStateMethod { get; }

    MethodInfo HandleAwaiterMethod { get; }

    MethodInfo GetHandleFieldMethod(Type fieldType);
}