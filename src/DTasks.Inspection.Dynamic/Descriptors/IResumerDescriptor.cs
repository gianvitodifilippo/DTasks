using System.Reflection;

namespace DTasks.Inspection.Dynamic.Descriptors;

internal interface IResumerDescriptor
{
    Type Type { get; }

    MethodInfo ResumeWithVoidMethod { get; }

    MethodInfo ResumeWithResultMethod { get; }

    IReaderDescriptor Reader { get; }
}
