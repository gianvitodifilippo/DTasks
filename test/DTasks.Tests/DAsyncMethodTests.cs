using DTasks.Hosting;

namespace DTasks;

public class DAsyncMethodTests
{
    [Fact]
    public async Task CreatedDTask_ShouldRanToCompletion_WhenMethodCompletesSynchronously()
    {
        // Arrange
        static async DTask Method()
        {
            await Task.CompletedTask;
        }

        // Act
        DTask sut = Method();
        DTaskStatus status = sut.Status;
        var awaiter = sut.GetDAwaiter();
        bool isCompleted = await awaiter.IsCompletedAsync();

        // Assert
        isCompleted.Should().BeTrue();
        status.Should().Be(DTaskStatus.RanToCompletion);
        sut.Status.Should().Be(DTaskStatus.RanToCompletion);
    }

    [Fact]
    public async Task CreatedDTask_ShouldBeRunning_WhenMethodIsAwaitingRegularAwaitable()
    {
        // Arrange
        static async DTask Method()
        {
            await Task.Delay(1000);
        }

        // Act
        DTask sut = Method();
        DTaskStatus status = sut.Status;
        var awaiter = sut.GetDAwaiter();
        bool isCompleted = await awaiter.IsCompletedAsync();

        // Assert
        isCompleted.Should().BeTrue();
        status.Should().Be(DTaskStatus.Running);
        sut.Status.Should().Be(DTaskStatus.RanToCompletion);
    }

    [Fact]
    public async Task CreatedDTask_ShouldBeSuspended_WhenMethodYields()
    {
        // Arrange
        static async DTask Method()
        {
            await Task.Delay(1000);
            await DTask.Yield();
        }

        // Act
        DTask sut = Method();
        DTaskStatus status = sut.Status;
        var awaiter = sut.GetDAwaiter();
        bool isCompleted = await awaiter.IsCompletedAsync();

        // Assert
        isCompleted.Should().BeFalse();
        status.Should().Be(DTaskStatus.Running);
        sut.Status.Should().Be(DTaskStatus.Suspended);
    }

    [Fact]
    public async Task CreatedDTask_ShouldSaveStateMachinesOfCallStack()
    {
        // Arrange
        static async DTask Parent()
        {
            await Child();
        }

        static async DTask Child()
        {
            await DTask.Yield();
        }

        var handler = Substitute.For<IStateHandler>();

        // Act
        DTask sut = Parent();
        var awaiter = sut.GetDAwaiter();
        await awaiter.IsCompletedAsync();
        awaiter.SaveState(ref handler);

        // Assert
        // handler.ReceivedWithAnyArgs(2).SaveStateMachine(ref Arg.Any<Arg.AnyType>(), Arg.Any<ISuspensionInfo>()); https://github.com/nsubstitute/NSubstitute/issues/787
        handler.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IStateHandler.SaveStateMachine))
            .Should()
            .HaveCount(2);
    }

    [Fact]
    public async Task IsSuspended_ShouldReturnTrue_WithCorrectAwaiterType_OtherwiseFalse()
    {
        // Arrange
        static async DTask Parent()
        {
            await Child();
        }

        static async DTask<TestResult> Child()
        {
            await DTask.Yield();
            return new();
        }

        // Act
        DTask sut = Parent();
        var awaiter = sut.GetDAwaiter();
        await awaiter.IsCompletedAsync();

        // Assert
        sut.As<IStateMachineInfo>().SuspendedAwaiterType.Should().Be<DTask<TestResult>.Awaiter>();
    }

    private sealed class TestResult;
}
