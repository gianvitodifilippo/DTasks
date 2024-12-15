namespace DTasks.Inspection.Dynamic.Descriptors;

internal interface IConverterDescriptorFactory
{
    IResumerDescriptor ResumerDescriptor { get; }

    ISuspenderDescriptor CreateSuspenderDescriptor(Type stateMachineType);
}
