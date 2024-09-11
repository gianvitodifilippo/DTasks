using Xunit.Sdk;
using static DTasks.Hosting.HostingFixtures;

namespace DTasks.Hosting;

public class BinaryDTaskHostTests
{
    private static readonly string _flowId = "flowId";

    private TestFlowStack _stack;
    private TestFlowHeap _heap;
    private readonly EquatableArray<byte> _heapBytes;
    private readonly TestDTaskStorage _storage;
    private readonly TestDTaskConverter _converter;
    private readonly TestBinaryDTaskHost _sut;

    public BinaryDTaskHostTests()
    {
        _stack = Substitute.For<TestFlowStack>();
        _heap = Substitute.For<TestFlowHeap>();
        _heapBytes = new byte[] { 1, 2, 3 };
        _storage = Substitute.For<TestDTaskStorage>();
        _converter = Substitute.For<TestDTaskConverter>();

        _converter
            .CreateHeap(Arg.Any<IDTaskScope>())
            .Returns(_heap);

        _converter
            .DeserializeHeap(_flowId, Arg.Any<IDTaskScope>(), _heapBytes)
            .Returns(_heap);

        _storage
            .CreateStack()
            .Returns(_stack);

        _stack
            .PopHeap()
            .Returns(_heapBytes);

        _sut = Substitute.For<TestBinaryDTaskHost>(_storage, _converter);
    }

    [Fact]
    public async Task SuspendAsync_ShouldSaveStackAndHeap()
    {
        // Arrange
        var scope = Substitute.For<IDTaskScope>();
        var suspendedTask = Substitute.For<TestSuspendedDTask>();

        _converter
            .SerializeHeap(ref _heap)
            .Returns(_heapBytes);

        // Act
        await _sut.SuspendAsync(_flowId, scope, suspendedTask.GetDAwaiter());

        // Assert
        _converter.Received(1).CreateHeap(scope);
        _converter.Received(1).SerializeHeap(ref _heap);
        _storage.Received(1).CreateStack();
        _stack.Received(1).PushHeap(_heapBytes);
        await _storage.Received(1).SaveStackAsync(_flowId, ref _stack, Arg.Any<CancellationToken>());
        // await task.Received(1).SuspendAsync(ref Arg.Any<ISuspensionHandler>(), Arg.Any<CancellationToken>()); https://github.com/nsubstitute/NSubstitute/issues/787
        suspendedTask.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(DTask.SuspendAsync))
            .Should()
            .HaveCount(1);
    }

    [Fact]
    public async Task ResumeAsync_ShouldSuspend_WhenFlowIsSuspended()
    {
        // Arrange
        var suspendedTask = Substitute.For<TestSuspendedDTask>();
        var scope = Substitute.For<IDTaskScope>();
        var stateMachineBytes = new byte[] { 4, 5, 6 };

        _converter
            .DeserializeStateMachine(_flowId, ref _heap, stateMachineBytes, Arg.Is<DTask>(task => task.IsCompleted))
            .Returns(suspendedTask);

        _storage
            .LoadStackAsync(_flowId, Arg.Any<CancellationToken>())
            .Returns(_stack);

        _stack
            .PopStateMachine(out _)
            .Returns(stateMachineBytes);

        // Act
        await _sut.ResumeAsync(_flowId, scope);

        // Assert
        _converter.Received(1).DeserializeHeap(_flowId, scope, _heapBytes);
        _ = _converter.Received(1).DeserializeStateMachine(_flowId, ref _heap, stateMachineBytes, Arg.Is<DTask>(task => task.IsCompleted));
        suspendedTask.ReceivedCalls()
            .Should()
            .ContainSingle(call => call.GetMethodInfo().Name == nameof(DTask.SaveState)); // suspendedTask.Received().SaveState(ref Arg.Any<IStateHandler>());
        suspendedTask.ReceivedCalls()
            .Should()
            .ContainSingle(call => call.GetMethodInfo().Name == nameof(DTask.SuspendAsync)); // await suspendedTask.Received().SuspendAsync(ref Arg.Any<ISuspensionHandler>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResumeAsync_ShouldComplete_WhenFlowIsCompleted()
    {
        // Arrange
        var completedTask = Substitute.For<TestCompletedDTask>();
        var scope = Substitute.For<IDTaskScope>();
        var stateMachineBytes = new byte[] { 4, 5, 6 };

        _converter
            .DeserializeStateMachine(_flowId, ref _heap, stateMachineBytes, Arg.Is<DTask>(task => task.IsCompleted))
            .Returns(completedTask);

        _storage
            .LoadStackAsync(_flowId, Arg.Any<CancellationToken>())
            .Returns(_stack);

        _stack
            .PopStateMachine(out _)
            .Returns(stateMachineBytes);

        // Act
        await _sut.ResumeAsync(_flowId, scope);

        // Assert
        _converter.Received(1).DeserializeHeap(_flowId, scope, _heapBytes);
        _ = _converter.Received(1).DeserializeStateMachine(_flowId, ref _heap, stateMachineBytes, Arg.Is<DTask>(task => task.IsCompleted));
        completedTask.ReceivedCalls()
            .Should()
            .ContainSingle(call => call.GetMethodInfo().Name == nameof(DTask.CompleteAsync));
    }

    [Fact]
    public async Task ResumeAsync_ShouldMoveNext_WhenStackContainsCompletedTask()
    {
        // Arrange
        var completedTask = Substitute.For<TestCompletedDTask>();
        var scope = Substitute.For<IDTaskScope>();
        var stateMachine1Bytes = new byte[] { 4, 5, 6 };
        var stateMachine2Bytes = new byte[] { 7, 8, 9 };

        _storage
            .LoadStackAsync(_flowId, Arg.Any<CancellationToken>())
            .Returns(_stack);

        _converter
            .DeserializeStateMachine(_flowId, ref _heap, stateMachine1Bytes, Arg.Is<DTask>(task => task.IsCompleted))
            .Returns(completedTask);

        _converter
            .DeserializeStateMachine(_flowId, ref _heap, stateMachine2Bytes, completedTask)
            .Returns(completedTask);

        _stack
            .PopStateMachine(out Arg.Any<bool>())
            .Returns(
                call => { call[0] = true; return stateMachine1Bytes; },
                call => { call[0] = false; return stateMachine2Bytes; },
                call => throw FailException.ForFailure("No more state machines to pop."));

        // Act
        await _sut.ResumeAsync(_flowId, scope);

        // Assert
        _stack.Received(2).PopStateMachine(out Arg.Any<bool>());
    }
}
