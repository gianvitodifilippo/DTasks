using FluentAssertions;
using System.Reflection;
using static DTasks.Inspection.Dynamic.InspectionFixtures;

namespace DTasks.Inspection.Dynamic.Descriptors;

public partial class ConverterDescriptorFactoryTests
{
    [Fact]
    public void TryCreate_CreatesDescriptors_WhenStateMachineConverterHasClassArgs()
    {
        // Arrange
        Type stateMachineType = typeof(object);
        Type suspenderGenericType = typeof(IStateMachineSuspender1<>);
        Type suspenderType = suspenderGenericType.MakeGenericType(stateMachineType);
        Type resumerType = typeof(IStateMachineResumer1);
        Type writerType = typeof(ClassWriter);
        Type readerType = typeof(ClassReader);

        MethodInfo suspendMethod = GetSuspendMethod(suspenderType, writerType);
        MethodInfo resumeWithVoidMethod = GetResumeWithVoidMethod(resumerType, readerType);
        MethodInfo resumeWithResultMethod = GetResumeWithResultMethod(resumerType, readerType);

        // Act
        bool result = ConverterDescriptorFactory.TryCreate(suspenderGenericType, resumerType, out ConverterDescriptorFactory? factory);
        ISuspenderDescriptor suspenderDescriptor = factory!.CreateSuspenderDescriptor(stateMachineType);
        IResumerDescriptor resumerDescriptor = factory!.ResumerDescriptor;

        // Assert
        result.Should().BeTrue();
        suspenderDescriptor.Type.Should().Be(suspenderType);
        suspenderDescriptor.SuspendMethod.Should().BeSameAs(suspendMethod);
        suspenderDescriptor.Writer.Type.Should().Be(writerType);
        suspenderDescriptor.Writer.GetWriteFieldMethod(typeof(int)).Should().BeSameAs(ClassWriter.WriteFieldMethod.MakeGenericMethod(typeof(int)));
        resumerDescriptor.Type.Should().Be(resumerType);
        resumerDescriptor.ResumeWithVoidMethod.Should().BeSameAs(resumeWithVoidMethod);
        resumerDescriptor.ResumeWithResultMethod.Should().BeSameAs(resumeWithResultMethod);
        resumerDescriptor.Reader.Type.Should().Be(readerType);
        resumerDescriptor.Reader.GetReadFieldMethod(typeof(int)).Should().BeSameAs(ClassReader.ReadFieldMethod.MakeGenericMethod(typeof(int)));
    }

    [Fact]
    public void TryCreate_CreatesDescriptor_WhenStateMachineConverterHasStructArgs()
    {
        // Arrange
        Type stateMachineType = typeof(object);
        Type suspenderGenericType = typeof(IStateMachineSuspender2<>);
        Type suspenderType = suspenderGenericType.MakeGenericType(stateMachineType);
        Type resumerType = typeof(IStateMachineResumer2);
        Type writerType = typeof(StructWriter);
        Type readerType = typeof(StructReader);

        MethodInfo suspendMethod = GetSuspendMethod(suspenderType, writerType);
        MethodInfo resumeWithVoidMethod = GetResumeWithVoidMethod(resumerType, readerType);
        MethodInfo resumeWithResultMethod = GetResumeWithResultMethod(resumerType, readerType);

        // Act
        bool result = ConverterDescriptorFactory.TryCreate(suspenderGenericType, resumerType, out ConverterDescriptorFactory? factory);
        ISuspenderDescriptor suspenderDescriptor = factory!.CreateSuspenderDescriptor(stateMachineType);
        IResumerDescriptor resumerDescriptor = factory!.ResumerDescriptor;

        // Assert
        result.Should().BeTrue();
        suspenderDescriptor.Type.Should().Be(suspenderType);
        suspenderDescriptor.SuspendMethod.Should().BeSameAs(suspendMethod);
        suspenderDescriptor.Writer.Type.Should().Be(writerType);
        suspenderDescriptor.Writer.GetWriteFieldMethod(typeof(int)).Should().BeSameAs(StructWriter.WriteFieldMethod.MakeGenericMethod(typeof(int)));
        resumerDescriptor.Type.Should().Be(resumerType);
        resumerDescriptor.ResumeWithVoidMethod.Should().BeSameAs(resumeWithVoidMethod);
        resumerDescriptor.ResumeWithResultMethod.Should().BeSameAs(resumeWithResultMethod);
        resumerDescriptor.Reader.Type.Should().Be(readerType);
        resumerDescriptor.Reader.GetReadFieldMethod(typeof(int)).Should().BeSameAs(StructReader.ReadFieldMethod.MakeGenericMethod(typeof(int)));
    }

    [Fact]
    public void TryCreate_CreatesDescriptor_WhenStateMachineConverterHasByRefStructArgs()
    {
        // Arrange
        Type stateMachineType = typeof(object);
        Type suspenderGenericType = typeof(IStateMachineSuspender3<>);
        Type suspenderType = suspenderGenericType.MakeGenericType(stateMachineType);
        Type resumerType = typeof(IStateMachineResumer3);
        Type writerType = typeof(StructWriter);
        Type readerType = typeof(StructReader);

        MethodInfo suspendMethod = GetSuspendMethod(suspenderType, writerType.MakeByRefType());
        MethodInfo resumeWithVoidMethod = GetResumeWithVoidMethod(resumerType, readerType.MakeByRefType());
        MethodInfo resumeWithResultMethod = GetResumeWithResultMethod(resumerType, readerType.MakeByRefType());

        // Act
        bool result = ConverterDescriptorFactory.TryCreate(suspenderGenericType, resumerType, out ConverterDescriptorFactory? factory);
        ISuspenderDescriptor suspenderDescriptor = factory!.CreateSuspenderDescriptor(stateMachineType);
        IResumerDescriptor resumerDescriptor = factory!.ResumerDescriptor;

        // Assert
        result.Should().BeTrue();
        suspenderDescriptor.Type.Should().Be(suspenderType);
        suspenderDescriptor.SuspendMethod.Should().BeSameAs(suspendMethod);
        suspenderDescriptor.Writer.Type.Should().Be(writerType);
        suspenderDescriptor.Writer.GetWriteFieldMethod(typeof(int)).Should().BeSameAs(StructWriter.WriteFieldMethod.MakeGenericMethod(typeof(int)));
        resumerDescriptor.Type.Should().Be(resumerType);
        resumerDescriptor.ResumeWithVoidMethod.Should().BeSameAs(resumeWithVoidMethod);
        resumerDescriptor.ResumeWithResultMethod.Should().BeSameAs(resumeWithResultMethod);
        resumerDescriptor.Reader.Type.Should().Be(readerType);
        resumerDescriptor.Reader.GetReadFieldMethod(typeof(int)).Should().BeSameAs(StructReader.ReadFieldMethod.MakeGenericMethod(typeof(int)));
    }

    [Fact]
    public void TryCreate_CreatesDescriptor_WhenReaderAndWriterHaveSpecializedMethods()
    {
        // Arrange
        Type stateMachineType = typeof(object);
        Type suspenderGenericType = typeof(IStateMachineSuspender4<>);
        Type suspenderType = suspenderGenericType.MakeGenericType(stateMachineType);
        Type resumerType = typeof(IStateMachineResumer4);
        Type writerType = typeof(WriterWithSpecializedMethod);
        Type readerType = typeof(ReaderWithSpecializedMethod);

        MethodInfo suspendMethod = GetSuspendMethod(suspenderType, writerType);
        MethodInfo resumeWithVoidMethod = GetResumeWithVoidMethod(resumerType, readerType);
        MethodInfo resumeWithResultMethod = GetResumeWithResultMethod(resumerType, readerType);

        // Act
        bool result = ConverterDescriptorFactory.TryCreate(suspenderGenericType, resumerType, out ConverterDescriptorFactory? factory);
        ISuspenderDescriptor suspenderDescriptor = factory!.CreateSuspenderDescriptor(stateMachineType);
        IResumerDescriptor resumerDescriptor = factory!.ResumerDescriptor;

        // Assert
        result.Should().BeTrue();
        suspenderDescriptor.Type.Should().Be(suspenderType);
        suspenderDescriptor.SuspendMethod.Should().BeSameAs(suspendMethod);
        suspenderDescriptor.Writer.Type.Should().Be(writerType);
        suspenderDescriptor.Writer.GetWriteFieldMethod(typeof(string)).Should().BeSameAs(WriterWithSpecializedMethod.WriteFieldMethod.MakeGenericMethod(typeof(string)));
        suspenderDescriptor.Writer.GetWriteFieldMethod(typeof(int)).Should().BeSameAs(WriterWithSpecializedMethod.SpecializedWriteFieldMethod);
        resumerDescriptor.Type.Should().Be(resumerType);
        resumerDescriptor.ResumeWithVoidMethod.Should().BeSameAs(resumeWithVoidMethod);
        resumerDescriptor.ResumeWithResultMethod.Should().BeSameAs(resumeWithResultMethod);
        resumerDescriptor.Reader.GetReadFieldMethod(typeof(string)).Should().BeSameAs(ReaderWithSpecializedMethod.ReadFieldMethod.MakeGenericMethod(typeof(string)));
        resumerDescriptor.Reader.GetReadFieldMethod(typeof(int)).Should().BeSameAs(ReaderWithSpecializedMethod.SpecializedReadFieldMethod);
        resumerDescriptor.Reader.Type.Should().Be(readerType);
    }
}
