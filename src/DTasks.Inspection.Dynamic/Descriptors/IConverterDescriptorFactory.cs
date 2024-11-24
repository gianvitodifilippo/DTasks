namespace DTasks.Inspection.Dynamic.Descriptors;

internal interface IConverterDescriptorFactory
{
    IConverterDescriptor CreateDescriptor(Type stateMachineType);
}
