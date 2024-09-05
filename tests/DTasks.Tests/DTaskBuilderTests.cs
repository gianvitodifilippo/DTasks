using System.Runtime.CompilerServices;

namespace DTasks;

public class DTaskBuilderTests
{
    [Fact]
    public void Create_ShouldCreateARunningDTask()
    {
        // Arrange
        var stateMachine = Substitute.For<IAsyncStateMachine>();

        // Act
        var sut = DTaskBuilder<VoidDTaskResult>.Create(ref stateMachine);

        // Assert
        sut.Status.Should().Be(DTaskStatus.Running);
    }

    [Fact]
    public void Create_ShouldSetTheStateMachine()
    {
        // Arrange
        var stateMachine = Substitute.For<IAsyncStateMachine>();

        // Act
        var sut = DTaskBuilder<VoidDTaskResult>.Create(ref stateMachine);

        // Assert
        stateMachine.Received().SetStateMachine((IAsyncStateMachine)sut);
    }
}
