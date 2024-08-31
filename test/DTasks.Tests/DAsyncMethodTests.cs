using DTasks.Host;

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
        DTask.DAwaiter dAwaiter = sut.GetDAwaiter();
        bool isCompleted = await dAwaiter.IsCompletedAsync();

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
        DTask.DAwaiter dAwaiter = sut.GetDAwaiter();
        bool isCompleted = await dAwaiter.IsCompletedAsync();

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
        DTask.DAwaiter dAwaiter = sut.GetDAwaiter();
        bool isCompleted = await dAwaiter.IsCompletedAsync();

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

        var handler = Substitute.For<ISuspensionHandler>();

        // Act
        DTask sut = Parent();
        DTask.DAwaiter dAwaiter = sut.GetDAwaiter();
        await dAwaiter.IsCompletedAsync();
        await dAwaiter.OnSuspendedAsync(ref handler);

        // Assert
        // handler.ReceivedWithAnyArgs(2).SaveStateMachine(ref Arg.Any<Arg.AnyType>(), Arg.Any<ISuspensionInfo>()); // https://github.com/nsubstitute/NSubstitute/issues/787
        handler.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(ISuspensionHandler.SaveStateMachine))
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

        DTask<TestResult>.Awaiter suspendedAwaiter = default;
        DTask.Awaiter nonSuspendedAwaiter = default;

        // Act
        DTask sut = Parent();
        DTask.DAwaiter dAwaiter = sut.GetDAwaiter();
        await dAwaiter.IsCompletedAsync();

        // Assert
        sut.As<ISuspensionInfo>().IsSuspended(ref suspendedAwaiter).Should().BeTrue();
        sut.As<ISuspensionInfo>().IsSuspended(ref nonSuspendedAwaiter).Should().BeFalse();
    }

    private sealed class TestResult { }
}
