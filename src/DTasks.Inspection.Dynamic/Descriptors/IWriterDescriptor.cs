using System.Reflection;

namespace DTasks.Inspection.Dynamic.Descriptors;

internal interface IWriterDescriptor
{
    Type Type { get; }

    MethodInfo GetWriteFieldMethod(Type fieldType);
}