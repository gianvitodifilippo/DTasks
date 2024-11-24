using System.Reflection;

namespace DTasks.Inspection.Dynamic.Descriptors;

internal sealed class ConverterDescriptor(
    Type type,
    MethodInfo suspendMethod,
    MethodInfo resumeWithVoidMethod,
    MethodInfo resumeWithResultMethod,
    IReaderDescriptor reader,
    IWriterDescriptor writer) : IConverterDescriptor
{
    public Type Type => type;

    public MethodInfo SuspendMethod => suspendMethod;

    public MethodInfo ResumeWithVoidMethod => resumeWithVoidMethod;

    public MethodInfo ResumeWithResultMethod => resumeWithResultMethod;

    public IReaderDescriptor Reader => reader;

    public IWriterDescriptor Writer => writer;
}
