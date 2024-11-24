using System.Reflection;

namespace DTasks.Inspection.Dynamic.Descriptors;

public interface IReaderDescriptor
{
    Type Type { get; }

    MethodInfo GetReadFieldMethod(Type fieldType);
}