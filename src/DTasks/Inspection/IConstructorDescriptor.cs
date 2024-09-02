using System.Reflection;

namespace DTasks.Inspection;

internal interface IConstructorDescriptor
{
    Type Type { get; }

    MethodInfo OnStateMethod { get; }

    MethodInfo OnAwaiterMethod { get; }

    MethodInfo GetOnFieldMethod(Type fieldType);
}