using System.Reflection;

namespace DTasks.Inspection.Dynamic.Descriptors;

public interface IWriterDescriptor
{
    Type Type { get; }

    MethodInfo GetWriteFieldMethod(Type fieldType);
}