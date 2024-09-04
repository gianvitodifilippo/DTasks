using System.Reflection;

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
        MethodInfo handleIntFieldMethod = GetRequiredMethod<ClassConstructor>("HandleField").MakeGenericMethod(typeof(int));
        MethodInfo handleAwaiterMethod = GetRequiredMethod<ClassConstructor>("HandleAwaiter");
        MethodInfo handleStringFieldMethod = GetRequiredMethod<ClassConstructor>("HandleField").MakeGenericMethod(typeof(string));

        // Act
        bool result = ConstructorDescriptor.TryCreate(typeof(ClassConstructor), out ConstructorDescriptor? sut);

        // Assert
        result.Should().BeTrue();
        sut.Should().NotBeNull();
        sut!.Type.Should().Be(typeof(ClassConstructor));
        sut.HandleStateMethod.Should().BeSameAs(handleIntFieldMethod);
        sut.HandleAwaiterMethod.Should().BeSameAs(handleAwaiterMethod);
        sut.GetHandleFieldMethod(typeof(string)).Should().BeSameAs(handleStringFieldMethod);
    }

    [Fact]
    public void Create_ShouldCreateDescriptor_ForConstructorWithHandleStateMethod()
    {
        // Arrange
        MethodInfo handleStateMethod = GetRequiredMethod<ConstructorWithHandleStateMethod>("HandleState");
        MethodInfo handleAwaiterMethod = GetRequiredMethod<ConstructorWithHandleStateMethod>("HandleAwaiter");
        MethodInfo handleStringFieldMethod = GetRequiredMethod<ConstructorWithHandleStateMethod>("HandleField").MakeGenericMethod(typeof(string));

        // Act
        bool result = ConstructorDescriptor.TryCreate(typeof(ConstructorWithHandleStateMethod), out ConstructorDescriptor? sut);

        // Assert
        result.Should().BeTrue();
        sut.Should().NotBeNull();
        sut!.Type.Should().Be(typeof(ConstructorWithHandleStateMethod));
        sut.HandleStateMethod.Should().BeSameAs(handleStateMethod);
        sut.HandleAwaiterMethod.Should().BeSameAs(handleAwaiterMethod);
        sut.GetHandleFieldMethod(typeof(string)).Should().BeSameAs(handleStringFieldMethod);
    }

    [Fact]
    public void Create_ShouldCreateDescriptor_ForConstructorWithSpecializedMethod()
    {
        // Arrange
        MethodInfo handleIntFieldMethod = GetRequiredMethod<ConstructorWithSpecializedMethod>("HandleField", [typeof(string), typeof(int).MakeByRefType()]);
        MethodInfo handleAwaiterMethod = GetRequiredMethod<ConstructorWithSpecializedMethod>("HandleAwaiter");
        MethodInfo handleStringFieldMethod = GetRequiredMethod<ConstructorWithSpecializedMethod>("HandleField", [typeof(string), Type.MakeGenericMethodParameter(0).MakeByRefType()]).MakeGenericMethod(typeof(string));

        // Act
        bool result = ConstructorDescriptor.TryCreate(typeof(ConstructorWithSpecializedMethod), out ConstructorDescriptor? sut);

        // Assert
        result.Should().BeTrue();
        sut.Should().NotBeNull();
        sut!.Type.Should().Be(typeof(ConstructorWithSpecializedMethod));
        sut.HandleStateMethod.Should().BeSameAs(handleIntFieldMethod);
        sut.HandleAwaiterMethod.Should().BeSameAs(handleAwaiterMethod);
        sut.GetHandleFieldMethod(typeof(string)).Should().BeSameAs(handleStringFieldMethod);
        sut.GetHandleFieldMethod(typeof(int)).Should().BeSameAs(handleIntFieldMethod);
    }

    [Fact]
    public void Create_ShouldCreateDescriptor_ForConstructorWithSpecializedMethodAndHandleStateMethod()
    {
        // Arrange
        MethodInfo handleStateMethod = GetRequiredMethod<ConstructorWithSpecializedMethodAndHandleStateMethod>("HandleState");
        MethodInfo handleAwaiterMethod = GetRequiredMethod<ConstructorWithSpecializedMethodAndHandleStateMethod>("HandleAwaiter");
        MethodInfo handleStringFieldMethod = GetRequiredMethod<ConstructorWithSpecializedMethodAndHandleStateMethod>("HandleField", [typeof(string), Type.MakeGenericMethodParameter(0).MakeByRefType()]).MakeGenericMethod(typeof(string));
        MethodInfo handleIntFieldMethod = GetRequiredMethod<ConstructorWithSpecializedMethodAndHandleStateMethod>("HandleField", [typeof(string), typeof(int).MakeByRefType()]);

        // Act
        bool result = ConstructorDescriptor.TryCreate(typeof(ConstructorWithSpecializedMethodAndHandleStateMethod), out ConstructorDescriptor? sut);

        // Assert
        result.Should().BeTrue();
        sut.Should().NotBeNull();
        sut!.Type.Should().Be(typeof(ConstructorWithSpecializedMethodAndHandleStateMethod));
        sut.HandleStateMethod.Should().BeSameAs(handleStateMethod);
        sut.HandleAwaiterMethod.Should().BeSameAs(handleAwaiterMethod);
        sut.GetHandleFieldMethod(typeof(string)).Should().BeSameAs(handleStringFieldMethod);
        sut.GetHandleFieldMethod(typeof(int)).Should().BeSameAs(handleIntFieldMethod);
    }

    [Fact]
    public void Create_ShouldNotCreateDescriptor_ForConstructorWithoutHandleField()
    {
        // Arrange

        // Act
        bool result = ConstructorDescriptor.TryCreate(typeof(ConstructorWithoutHandleField), out ConstructorDescriptor? sut);

        // Assert
        result.Should().BeFalse();
        sut.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldNotCreateDescriptor_ForConstructorWithoutHandleAwaiter()
    {
        // Arrange

        // Act
        bool result = ConstructorDescriptor.TryCreate(typeof(ConstructorWithoutHandleAwaiter), out ConstructorDescriptor? sut);

        // Assert
        result.Should().BeFalse();
        sut.Should().BeNull();
    }
}
