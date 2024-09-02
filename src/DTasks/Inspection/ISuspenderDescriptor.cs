using System.Reflection;

namespace DTasks.Inspection;

internal interface ISuspenderDescriptor
{
    Type DelegateType { get; }

    ParameterInfo DeconstructorParameter { get; }

    IDeconstructorDescriptor DeconstructorDescriptor { get; }
}