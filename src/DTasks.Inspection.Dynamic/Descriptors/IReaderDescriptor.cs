using System.Reflection;

namespace DTasks.Inspection.Dynamic.Descriptors;

internal interface IReaderDescriptor
{
    Type Type { get; }

    MethodInfo GetReadFieldMethod(Type fieldType);
}