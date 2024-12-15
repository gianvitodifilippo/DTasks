using System.Reflection;

namespace DTasks.Inspection.Dynamic.Descriptors;

internal sealed class SuspenderDescriptor(
    Type type,
    MethodInfo suspendMethod,
    IWriterDescriptor writer) : ISuspenderDescriptor
{
    public Type Type => type;

    public MethodInfo SuspendMethod => suspendMethod;

    public IWriterDescriptor Writer => writer;
}
