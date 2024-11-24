using FluentAssertions;
using static DTasks.Inspection.Dynamic.InspectionFixtures;

namespace DTasks.Inspection.Dynamic.Descriptors;

public partial class ConverterDescriptorFactoryTests
{
    [Fact]
    public void TryCreate_CreatesDescriptor_WhenStateMachineConverterHasClassArgs()
    {
        // Arrange
        Type stateMachineType = typeof(object);
        Type converterGenericType = typeof(IStateMachineConverter1<>);
        Type converterType = converterGenericType.MakeGenericType(stateMachineType);

        // Act
        bool result = ConverterDescriptorFactory.TryCreate(converterGenericType, out ConverterDescriptorFactory? factory);
        IConverterDescriptor descriptor = factory!.CreateDescriptor(stateMachineType);

        // Assert
        result.Should().BeTrue();
        descriptor!.Type.Should().Be(converterType);
        descriptor.SuspendMethod.Should().BeSameAs(IStateMachineConverter1<object>.SuspendMethod);
        descriptor.ResumeWithVoidMethod.Should().BeSameAs(IStateMachineConverter1<object>.ResumeWithVoidMethod);
        descriptor.ResumeWithResultMethod.Should().BeSameAs(IStateMachineConverter1<object>.ResumeWithResultMethod);
        descriptor.Reader.Type.Should().Be<ClassReader>();
        descriptor.Reader.GetReadFieldMethod(typeof(int)).Should().BeSameAs(ClassReader.ReadFieldMethod.MakeGenericMethod(typeof(int)));
        descriptor.Writer.Type.Should().Be<ClassWriter>();
        descriptor.Writer.GetWriteFieldMethod(typeof(int)).Should().BeSameAs(ClassWriter.WriteFieldMethod.MakeGenericMethod(typeof(int)));
    }

    [Fact]
    public void TryCreate_CreatesDescriptor_WhenStateMachineConverterHasStructArgs()
    {
        // Arrange
        Type stateMachineType = typeof(object);
        Type converterGenericType = typeof(IStateMachineConverter2<>);
        Type converterType = converterGenericType.MakeGenericType(stateMachineType);

        // Act
        bool result = ConverterDescriptorFactory.TryCreate(converterGenericType, out ConverterDescriptorFactory? factory);
        IConverterDescriptor descriptor = factory!.CreateDescriptor(stateMachineType);

        // Assert
        result.Should().BeTrue();
        descriptor!.Type.Should().Be(converterType);
        descriptor.SuspendMethod.Should().BeSameAs(IStateMachineConverter2<object>.SuspendMethod);
        descriptor.ResumeWithVoidMethod.Should().BeSameAs(IStateMachineConverter2<object>.ResumeWithVoidMethod);
        descriptor.ResumeWithResultMethod.Should().BeSameAs(IStateMachineConverter2<object>.ResumeWithResultMethod);
        descriptor.Reader.Type.Should().Be<StructReader>();
        descriptor.Reader.GetReadFieldMethod(typeof(int)).Should().BeSameAs(StructReader.ReadFieldMethod.MakeGenericMethod(typeof(int)));
        descriptor.Writer.Type.Should().Be<StructWriter>();
        descriptor.Writer.GetWriteFieldMethod(typeof(int)).Should().BeSameAs(StructWriter.WriteFieldMethod.MakeGenericMethod(typeof(int)));
    }

    [Fact]
    public void TryCreate_CreatesDescriptor_WhenStateMachineConverterHasByRefStructArgs()
    {
        // Arrange
        Type stateMachineType = typeof(object);
        Type converterGenericType = typeof(IStateMachineConverter3<>);
        Type converterType = converterGenericType.MakeGenericType(stateMachineType);

        // Act
        bool result = ConverterDescriptorFactory.TryCreate(converterGenericType, out ConverterDescriptorFactory? factory);
        IConverterDescriptor descriptor = factory!.CreateDescriptor(stateMachineType);

        // Assert
        result.Should().BeTrue();
        descriptor!.Type.Should().Be(converterType);
        descriptor.SuspendMethod.Should().BeSameAs(IStateMachineConverter3<object>.SuspendMethod);
        descriptor.ResumeWithVoidMethod.Should().BeSameAs(IStateMachineConverter3<object>.ResumeWithVoidMethod);
        descriptor.ResumeWithResultMethod.Should().BeSameAs(IStateMachineConverter3<object>.ResumeWithResultMethod);
        descriptor.Reader.Type.Should().Be<StructReader>();
        descriptor.Reader.GetReadFieldMethod(typeof(int)).Should().BeSameAs(StructReader.ReadFieldMethod.MakeGenericMethod(typeof(int)));
        descriptor.Writer.Type.Should().Be<StructWriter>();
        descriptor.Writer.GetWriteFieldMethod(typeof(int)).Should().BeSameAs(StructWriter.WriteFieldMethod.MakeGenericMethod(typeof(int)));
    }

    [Fact]
    public void TryCreate_CreatesDescriptor_WhenReaderAndWriterHaveSpecializedMethods()
    {
        // Arrange
        Type stateMachineType = typeof(object);
        Type converterGenericType = typeof(IStateMachineConverter4<>);
        Type converterType = converterGenericType.MakeGenericType(stateMachineType);

        // Act
        bool result = ConverterDescriptorFactory.TryCreate(converterGenericType, out ConverterDescriptorFactory? factory);
        IConverterDescriptor descriptor = factory!.CreateDescriptor(stateMachineType);

        // Assert
        result.Should().BeTrue();
        descriptor!.Type.Should().Be(converterType);
        descriptor.SuspendMethod.Should().BeSameAs(IStateMachineConverter4<object>.SuspendMethod);
        descriptor.ResumeWithVoidMethod.Should().BeSameAs(IStateMachineConverter4<object>.ResumeWithVoidMethod);
        descriptor.ResumeWithResultMethod.Should().BeSameAs(IStateMachineConverter4<object>.ResumeWithResultMethod);
        descriptor.Reader.Type.Should().Be<ReaderWithSpecializedMethod>();
        descriptor.Reader.GetReadFieldMethod(typeof(string)).Should().BeSameAs(ReaderWithSpecializedMethod.ReadFieldMethod.MakeGenericMethod(typeof(string)));
        descriptor.Reader.GetReadFieldMethod(typeof(int)).Should().BeSameAs(ReaderWithSpecializedMethod.SpecializedReadFieldMethod);
        descriptor.Writer.Type.Should().Be<WriterWithSpecializedMethod>();
        descriptor.Writer.GetWriteFieldMethod(typeof(string)).Should().BeSameAs(WriterWithSpecializedMethod.WriteFieldMethod.MakeGenericMethod(typeof(string)));
        descriptor.Writer.GetWriteFieldMethod(typeof(int)).Should().BeSameAs(WriterWithSpecializedMethod.SpecializedWriteFieldMethod);
    }
}
