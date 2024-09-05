using System.Reflection;

namespace DTasks.Inspection;

internal interface IResumerDescriptor
{
    Type DelegateType { get; }

    ParameterInfo ConstructorParameter { get; }

    IConstructorDescriptor ConstructorDescriptor { get; }
}
