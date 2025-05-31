using System.Reflection;

namespace DTasks.Inspection.Dynamic.Descriptors;

internal sealed class ResumerDescriptor(
    Type type,
    MethodInfo resumeWithVoidMethod,
    MethodInfo resumeWithResultMethod,
    MethodInfo resumeWithExceptionMethod,
    IReaderDescriptor reader) : IResumerDescriptor
{
    public Type Type => type;

    public MethodInfo ResumeWithVoidMethod => resumeWithVoidMethod;

    public MethodInfo ResumeWithResultMethod => resumeWithResultMethod;

    public MethodInfo ResumeWithExceptionMethod => resumeWithExceptionMethod;

    public IReaderDescriptor Reader => reader;
}
