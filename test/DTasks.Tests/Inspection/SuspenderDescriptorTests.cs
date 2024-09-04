namespace DTasks.Inspection;

public partial class SuspenderDescriptorTests
{
    [Theory]
    [InlineData(typeof(StructSuspender<>), typeof(StructDeconstructor), false)]
    [InlineData(typeof(ClassSuspender<>), typeof(ClassDeconstructor), false)]
    [InlineData(typeof(ByRefStructSuspender<>), typeof(StructDeconstructor), true)]
    [InlineData(typeof(ByRefClassSuspender<>), typeof(ClassDeconstructor), true)]
    public void TryCreate_ShouldCreateDescriptor_ForAllSuspenders(Type suspenderType, Type parameterType, bool byRef)
    {
        // Arrange

        // Act
        bool result = SuspenderDescriptor.TryCreate(suspenderType, out SuspenderDescriptor? sut);

        // Assert
        result.Should().BeTrue();
        sut.Should().NotBeNull();
        sut!.DelegateType.Should().Be(suspenderType);
        sut.DeconstructorParameter.ParameterType.Should().Be(byRef ? parameterType.MakeByRefType() : parameterType);
    }

    [Theory]
    [InlineData(typeof(SuspenderWithoutHandleField<>))]
    [InlineData(typeof(SuspenderWithoutHandleAwaiter<>))]
    public void TryCreate_ShouldNotCreateDescriptor_ForInvalidSuspenders(Type suspenderType)
    {
        // Arrange

        // Act
        bool result = SuspenderDescriptor.TryCreate(suspenderType, out SuspenderDescriptor? sut);

        // Assert
        result.Should().BeFalse();
        sut.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldCreateDescriptor_ForMinimalDeconstructor()
    {
        // Arrange

        // Act
        bool result = DeconstructorDescriptor.TryCreate(typeof(ClassDeconstructor), out DeconstructorDescriptor? sut);

        // Assert
        result.Should().BeTrue();
        sut.Should().NotBeNull();
        sut!.Type.Should().Be(typeof(ClassDeconstructor));
        sut.HandleStateMethod.Should().BeSameAs(GetRequiredMethod<ClassDeconstructor>("HandleField").MakeGenericMethod(typeof(int)));
        sut.HandleAwaiterMethod.Should().BeSameAs(GetRequiredMethod<ClassDeconstructor>("HandleAwaiter"));
        sut.GetHandleFieldMethod(typeof(string)).Should().BeSameAs(GetRequiredMethod<ClassDeconstructor>("HandleField").MakeGenericMethod(typeof(string)));
    }

    [Fact]
    public void Create_ShouldCreateDescriptor_ForDeconstructorWithHandleStateMethod()
    {
        // Arrange

        // Act
        bool result = DeconstructorDescriptor.TryCreate(typeof(DeconstructorWithHandleStateMethod), out DeconstructorDescriptor? sut);

        // Assert
        result.Should().BeTrue();
        sut.Should().NotBeNull();
        sut!.Type.Should().Be(typeof(DeconstructorWithHandleStateMethod));
        sut.HandleStateMethod.Should().BeSameAs(GetRequiredMethod<DeconstructorWithHandleStateMethod>("HandleState"));
        sut.HandleAwaiterMethod.Should().BeSameAs(GetRequiredMethod<DeconstructorWithHandleStateMethod>("HandleAwaiter"));
        sut.GetHandleFieldMethod(typeof(string)).Should().BeSameAs(GetRequiredMethod<DeconstructorWithHandleStateMethod>("HandleField").MakeGenericMethod(typeof(string)));
    }

    [Fact]
    public void Create_ShouldCreateDescriptor_ForDeconstructorWithSpecializedMethod()
    {
        // Arrange

        // Act
        bool result = DeconstructorDescriptor.TryCreate(typeof(DeconstructorWithSpecializedMethod), out DeconstructorDescriptor? sut);

        // Assert
        result.Should().BeTrue();
        sut.Should().NotBeNull();
        sut!.Type.Should().Be(typeof(DeconstructorWithSpecializedMethod));
        sut.HandleStateMethod.Should().BeSameAs(GetRequiredMethod<DeconstructorWithSpecializedMethod>("HandleField", [typeof(string), typeof(int)]));
        sut.HandleAwaiterMethod.Should().BeSameAs(GetRequiredMethod<DeconstructorWithSpecializedMethod>("HandleAwaiter"));
        sut.GetHandleFieldMethod(typeof(string)).Should().BeSameAs(
          GetRequiredMethod<DeconstructorWithSpecializedMethod>("HandleField", [typeof(string), Type.MakeGenericMethodParameter(0)]).MakeGenericMethod(typeof(string)));
        sut.GetHandleFieldMethod(typeof(int)).Should().BeSameAs(GetRequiredMethod<DeconstructorWithSpecializedMethod>("HandleField", [typeof(string), typeof(int)]));
    }

    [Fact]
    public void Create_ShouldCreateDescriptor_ForDeconstructorWithSpecializedMethodAndHandleStateMethod()
    {
        // Arrange

        // Act
        bool result = DeconstructorDescriptor.TryCreate(typeof(DeconstructorWithSpecializedMethodAndHandleStateMethod), out DeconstructorDescriptor? sut);

        // Assert
        result.Should().BeTrue();
        sut.Should().NotBeNull();
        sut!.Type.Should().Be(typeof(DeconstructorWithSpecializedMethodAndHandleStateMethod));
        sut.HandleStateMethod.Should().BeSameAs(GetRequiredMethod<DeconstructorWithSpecializedMethodAndHandleStateMethod>("HandleState"));
        sut.HandleAwaiterMethod.Should().BeSameAs(GetRequiredMethod<DeconstructorWithSpecializedMethodAndHandleStateMethod>("HandleAwaiter"));
        sut.GetHandleFieldMethod(typeof(string)).Should().BeSameAs(
          GetRequiredMethod<DeconstructorWithSpecializedMethodAndHandleStateMethod>("HandleField", [typeof(string), Type.MakeGenericMethodParameter(0)]).MakeGenericMethod(typeof(string)));
        sut.GetHandleFieldMethod(typeof(int)).Should().BeSameAs(GetRequiredMethod<DeconstructorWithSpecializedMethodAndHandleStateMethod>("HandleField", [typeof(string), typeof(int)]));
    }

    [Fact]
    public void Create_ShouldNotCreateDescriptor_ForDeconstructorWithoutHandleField()
    {
        // Arrange

        // Act
        bool result = DeconstructorDescriptor.TryCreate(typeof(DeconstructorWithoutHandleField), out DeconstructorDescriptor? sut);

        // Assert
        result.Should().BeFalse();
        sut.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldNotCreateDescriptor_ForDeconstructorWithoutHandleAwaiter()
    {
        // Arrange

        // Act
        bool result = DeconstructorDescriptor.TryCreate(typeof(DeconstructorWithoutHandleAwaiter), out DeconstructorDescriptor? sut);

        // Assert
        result.Should().BeFalse();
        sut.Should().BeNull();
    }
}
