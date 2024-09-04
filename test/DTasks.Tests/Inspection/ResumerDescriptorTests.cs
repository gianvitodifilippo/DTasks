using System.Reflection;
using static DTasks.Inspection.InspectionConstants;

namespace DTasks.Inspection;

public partial class ResumerDescriptorTests
{
    [Theory]
    [InlineData(typeof(StructResumer), typeof(StructConstructor), false)]
    [InlineData(typeof(ClassResumer), typeof(ClassConstructor), false)]
    [InlineData(typeof(ByRefStructResumer), typeof(StructConstructor), true)]
    [InlineData(typeof(ByRefClassResumer), typeof(ClassConstructor), true)]
    public void TryCreate_ShouldCreateDescriptor_ForAllResumers(Type resumerType, Type parameterType, bool byRef)
    {
        // Arrange
        Type constructorParameterType = byRef ? parameterType.MakeByRefType() : parameterType;

        // Act
        bool result = ResumerDescriptor.TryCreate(resumerType, out ResumerDescriptor? sut);

        // Assert
        result.Should().BeTrue();
        sut.Should().NotBeNull();
        sut!.DelegateType.Should().Be(resumerType);
        sut.ConstructorParameter.ParameterType.Should().Be(constructorParameterType);
    }

    [Theory]
    [InlineData(typeof(ResumerWithoutHandleField))]
    [InlineData(typeof(ResumerWithoutHandleAwaiter))]
    public void TryCreate_ShouldNotCreateDescriptor_ForInvalidResumers(Type resumerType)
    {
        // Arrange

        // Act
        bool result = ResumerDescriptor.TryCreate(resumerType, out ResumerDescriptor? sut);

        // Assert
        result.Should().BeFalse();
        sut.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldCreateDescriptor_ForMinimalConstructor()
    {
        // Arrange
        Type constructorType = typeof(ClassConstructor);
        MethodInfo handleFieldMethodOfInt = GetRequiredMethod<ClassConstructor>(MethodNames.HandleField).MakeGenericMethod(typeof(int));
        MethodInfo handleAwaiterMethod = GetRequiredMethod<ClassConstructor>(MethodNames.HandleAwaiter);
        MethodInfo handleFieldMethodOfString = GetRequiredMethod<ClassConstructor>(MethodNames.HandleField).MakeGenericMethod(typeof(string));

        // Act
        bool result = ConstructorDescriptor.TryCreate(constructorType, out ConstructorDescriptor? sut);

        // Assert
        result.Should().BeTrue();
        sut.Should().NotBeNull();
        sut!.Type.Should().Be(constructorType);
        sut.HandleStateMethod.Should().BeSameAs(handleFieldMethodOfInt);
        sut.HandleAwaiterMethod.Should().BeSameAs(handleAwaiterMethod);
        sut.GetHandleFieldMethod(typeof(string)).Should().BeSameAs(handleFieldMethodOfString);
    }

    [Fact]
    public void Create_ShouldCreateDescriptor_ForConstructorWithHandleStateMethod()
    {
        // Arrange
        Type constructorType = typeof(ConstructorWithHandleStateMethod);
        MethodInfo handleStateMethod = GetRequiredMethod<ConstructorWithHandleStateMethod>(MethodNames.HandleState);
        MethodInfo handleAwaiterMethod = GetRequiredMethod<ConstructorWithHandleStateMethod>(MethodNames.HandleAwaiter);
        MethodInfo handleFieldMethodOfString = GetRequiredMethod<ConstructorWithHandleStateMethod>(MethodNames.HandleField).MakeGenericMethod(typeof(string));

        // Act
        bool result = ConstructorDescriptor.TryCreate(constructorType, out ConstructorDescriptor? sut);

        // Assert
        result.Should().BeTrue();
        sut.Should().NotBeNull();
        sut!.Type.Should().Be(constructorType);
        sut.HandleStateMethod.Should().BeSameAs(handleStateMethod);
        sut.HandleAwaiterMethod.Should().BeSameAs(handleAwaiterMethod);
        sut.GetHandleFieldMethod(typeof(string)).Should().BeSameAs(handleFieldMethodOfString);
    }

    [Fact]
    public void Create_ShouldCreateDescriptor_ForConstructorWithSpecializedMethod()
    {
        // Arrange
        Type constructorType = typeof(ConstructorWithSpecializedMethod);
        MethodInfo handleIntFieldMethod = GetRequiredMethod<ConstructorWithSpecializedMethod>(MethodNames.HandleField, [typeof(string), typeof(int).MakeByRefType()]);
        MethodInfo handleAwaiterMethod = GetRequiredMethod<ConstructorWithSpecializedMethod>(MethodNames.HandleAwaiter);
        MethodInfo handleFieldMethodOfString = GetRequiredMethod<ConstructorWithSpecializedMethod>(MethodNames.HandleField, [typeof(string), Type.MakeGenericMethodParameter(0).MakeByRefType()]).MakeGenericMethod(typeof(string));

        // Act
        bool result = ConstructorDescriptor.TryCreate(constructorType, out ConstructorDescriptor? sut);

        // Assert
        result.Should().BeTrue();
        sut.Should().NotBeNull();
        sut!.Type.Should().Be(constructorType);
        sut.HandleStateMethod.Should().BeSameAs(handleIntFieldMethod);
        sut.HandleAwaiterMethod.Should().BeSameAs(handleAwaiterMethod);
        sut.GetHandleFieldMethod(typeof(string)).Should().BeSameAs(handleFieldMethodOfString);
        sut.GetHandleFieldMethod(typeof(int)).Should().BeSameAs(handleIntFieldMethod);
    }

    [Fact]
    public void Create_ShouldCreateDescriptor_ForConstructorWithSpecializedMethodAndHandleStateMethod()
    {
        // Arrange
        Type constructorType = typeof(ConstructorWithSpecializedMethodAndHandleStateMethod);
        MethodInfo handleStateMethod = GetRequiredMethod<ConstructorWithSpecializedMethodAndHandleStateMethod>(MethodNames.HandleState);
        MethodInfo handleAwaiterMethod = GetRequiredMethod<ConstructorWithSpecializedMethodAndHandleStateMethod>(MethodNames.HandleAwaiter);
        MethodInfo handleFieldMethodOfString = GetRequiredMethod<ConstructorWithSpecializedMethodAndHandleStateMethod>(MethodNames.HandleField, [typeof(string), Type.MakeGenericMethodParameter(0).MakeByRefType()]).MakeGenericMethod(typeof(string));
        MethodInfo handleIntFieldMethod = GetRequiredMethod<ConstructorWithSpecializedMethodAndHandleStateMethod>(MethodNames.HandleField, [typeof(string), typeof(int).MakeByRefType()]);

        // Act
        bool result = ConstructorDescriptor.TryCreate(constructorType, out ConstructorDescriptor? sut);

        // Assert
        result.Should().BeTrue();
        sut.Should().NotBeNull();
        sut!.Type.Should().Be(constructorType);
        sut.HandleStateMethod.Should().BeSameAs(handleStateMethod);
        sut.HandleAwaiterMethod.Should().BeSameAs(handleAwaiterMethod);
        sut.GetHandleFieldMethod(typeof(string)).Should().BeSameAs(handleFieldMethodOfString);
        sut.GetHandleFieldMethod(typeof(int)).Should().BeSameAs(handleIntFieldMethod);
    }

    [Fact]
    public void Create_ShouldNotCreateDescriptor_ForConstructorWithoutHandleField()
    {
        // Arrange
        Type constructorType = typeof(ConstructorWithoutHandleField);

        // Act
        bool result = ConstructorDescriptor.TryCreate(constructorType, out ConstructorDescriptor? sut);

        // Assert
        result.Should().BeFalse();
        sut.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldNotCreateDescriptor_ForConstructorWithoutHandleAwaiter()
    {
        // Arrange
        Type constructorType = typeof(ConstructorWithoutHandleAwaiter);

        // Act
        bool result = ConstructorDescriptor.TryCreate(constructorType, out ConstructorDescriptor? sut);

        // Assert
        result.Should().BeFalse();
        sut.Should().BeNull();
    }
}
