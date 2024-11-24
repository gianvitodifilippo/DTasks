using System.Reflection;

namespace DTasks.Inspection.Dynamic.Descriptors;

internal interface IConverterDescriptor
{
    Type Type { get; }

    MethodInfo SuspendMethod { get; }

    MethodInfo ResumeWithVoidMethod { get; }

    MethodInfo ResumeWithResultMethod { get; }

    IReaderDescriptor Reader { get; }

    IWriterDescriptor Writer { get; }
}
