using System.Reflection;

namespace DTasks.Inspection.Dynamic.Descriptors;

internal interface ISuspenderDescriptor
{
    Type Type { get; }

    MethodInfo SuspendMethod { get; }

    IWriterDescriptor Writer { get; }
}
