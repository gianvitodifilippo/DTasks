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
        MethodInfo handleFieldMethod = constructorType.GetRequiredMethod(
            name: MethodNames.HandleField,
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            parameterTypes: [typeof(string), Type.MakeGenericMethodParameter(0).MakeByRefType()]);
        MethodInfo handleAwaiterMethod = constructorType.GetRequiredMethod(
            name: MethodNames.HandleAwaiter,
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            parameterTypes: [typeof(string)]);

        // Act
        bool result = ConstructorDescriptor.TryCreate(constructorType, out ConstructorDescriptor? sut);

        // Assert
        result.Should().BeTrue();
        sut.Should().NotBeNull();
        sut!.Type.Should().Be(constructorType);
        sut.HandleStateMethod.Should().BeSameAs(handleFieldMethod.MakeGenericMethod(typeof(int)));
        sut.HandleAwaiterMethod.Should().BeSameAs(handleAwaiterMethod);
        sut.GetHandleFieldMethod(typeof(string)).Should().BeSameAs(handleFieldMethod.MakeGenericMethod(typeof(string)));
    }

    [Fact]
    public void Create_ShouldCreateDescriptor_ForConstructorWithHandleStateMethod()
    {
        // Arrange
        Type constructorType = typeof(ConstructorWithHandleStateMethod);
        MethodInfo handleStateMethod = constructorType.GetRequiredMethod(
            name: MethodNames.HandleState,
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            parameterTypes: [typeof(string), typeof(int).MakeByRefType()]);
        MethodInfo handleAwaiterMethod = constructorType.GetRequiredMethod(
            name: MethodNames.HandleAwaiter,
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            parameterTypes: [typeof(string)]);
        MethodInfo handleFieldMethod = constructorType.GetRequiredMethod(
            name: MethodNames.HandleField,
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            parameterTypes: [typeof(string), Type.MakeGenericMethodParameter(0).MakeByRefType()]);

        // Act
        bool result = ConstructorDescriptor.TryCreate(constructorType, out ConstructorDescriptor? sut);

        // Assert
        result.Should().BeTrue();
        sut.Should().NotBeNull();
        sut!.Type.Should().Be(constructorType);
        sut.HandleStateMethod.Should().BeSameAs(handleStateMethod);
        sut.HandleAwaiterMethod.Should().BeSameAs(handleAwaiterMethod);
        sut.GetHandleFieldMethod(typeof(string)).Should().BeSameAs(handleFieldMethod.MakeGenericMethod(typeof(string)));
    }

    [Fact]
    public void Create_ShouldCreateDescriptor_ForConstructorWithSpecializedMethod()
    {
        // Arrange
        Type constructorType = typeof(ConstructorWithSpecializedMethod);
        MethodInfo handleIntFieldMethod = constructorType.GetRequiredMethod(
            name: MethodNames.HandleField,
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            parameterTypes: [typeof(string), typeof(int).MakeByRefType()]);
        MethodInfo handleAwaiterMethod = constructorType.GetRequiredMethod(
            name: MethodNames.HandleAwaiter,
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            parameterTypes: [typeof(string)]);
        MethodInfo handleFieldMethod = constructorType.GetRequiredMethod(
            name: MethodNames.HandleField,
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            parameterTypes: [typeof(string), Type.MakeGenericMethodParameter(0).MakeByRefType()]);

        // Act
        bool result = ConstructorDescriptor.TryCreate(constructorType, out ConstructorDescriptor? sut);

        // Assert
        result.Should().BeTrue();
        sut.Should().NotBeNull();
        sut!.Type.Should().Be(constructorType);
        sut.HandleStateMethod.Should().BeSameAs(handleIntFieldMethod);
        sut.HandleAwaiterMethod.Should().BeSameAs(handleAwaiterMethod);
        sut.GetHandleFieldMethod(typeof(string)).Should().BeSameAs(handleFieldMethod.MakeGenericMethod(typeof(string)));
        sut.GetHandleFieldMethod(typeof(int)).Should().BeSameAs(handleIntFieldMethod);
    }

    [Fact]
    public void Create_ShouldCreateDescriptor_ForConstructorWithSpecializedMethodAndHandleStateMethod()
    {
        // Arrange
        Type constructorType = typeof(ConstructorWithSpecializedMethodAndHandleStateMethod);
        MethodInfo handleStateMethod = constructorType.GetRequiredMethod(
            name: MethodNames.HandleState,
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            parameterTypes: [typeof(string), typeof(int).MakeByRefType()]);
        MethodInfo handleAwaiterMethod = constructorType.GetRequiredMethod(
            name: MethodNames.HandleAwaiter,
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            parameterTypes: [typeof(string)]);
        MethodInfo handleFieldMethod = constructorType.GetRequiredMethod(
            name: MethodNames.HandleField,
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            parameterTypes: [typeof(string), Type.MakeGenericMethodParameter(0).MakeByRefType()]);
        MethodInfo handleIntFieldMethod = constructorType.GetRequiredMethod(
            name: MethodNames.HandleField,
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            parameterTypes: [typeof(string), typeof(int).MakeByRefType()]);

        // Act
        bool result = ConstructorDescriptor.TryCreate(constructorType, out ConstructorDescriptor? sut);

        // Assert
        result.Should().BeTrue();
        sut.Should().NotBeNull();
        sut!.Type.Should().Be(constructorType);
        sut.HandleStateMethod.Should().BeSameAs(handleStateMethod);
        sut.HandleAwaiterMethod.Should().BeSameAs(handleAwaiterMethod);
        sut.GetHandleFieldMethod(typeof(string)).Should().BeSameAs(handleFieldMethod.MakeGenericMethod(typeof(string)));
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
