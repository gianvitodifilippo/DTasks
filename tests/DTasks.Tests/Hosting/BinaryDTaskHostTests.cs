﻿using static DTasks.Hosting.HostingFixtures;

namespace DTasks.Hosting;

public class BinaryDTaskHostTests
{
    private static readonly string s_flowId = "flowId";
    private static readonly byte[] s_heapBytes = [1, 2, 3];
    private static readonly byte[] s_stateMachine1Bytes = [4, 5, 6];
    private static readonly byte[] s_stateMachine2Bytes = [7, 8, 9];
    private static readonly byte[] s_emptyBytes = [];

    private TestFlowStack _stack;
    private TestFlowHeap _heap;
    private readonly TestDTaskStorage _storage;
    private readonly TestDTaskConverter _converter;
    private readonly TestBinaryDTaskHost _sut;

    public BinaryDTaskHostTests()
    {
        _stack = Substitute.For<TestFlowStack>();
        _heap = Substitute.For<TestFlowHeap>();
        _storage = Substitute.For<TestDTaskStorage>();
        _converter = Substitute.For<TestDTaskConverter>();

        _converter
            .CreateHeap(Arg.Any<IDTaskScope>())
            .Returns(_heap);

        _converter
            .DeserializeHeap(s_flowId, Arg.Any<IDTaskScope>(), s_heapBytes)
            .Returns(_heap);

        _storage
            .CreateStack()
            .Returns(_stack);

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
            .Returns(s_heapBytes);

        // Act
        await _sut.SuspendAsync(s_flowId, scope, suspendedTask.GetDAwaiter());

        // Assert
        _converter.Received(1).CreateHeap(scope);
        _converter.Received(1).SerializeHeap(ref _heap);
        _storage.Received(1).CreateStack();
        _stack.Received(1).Push(s_heapBytes);
        await _storage.Received(1).SaveStackAsync(s_flowId, ref _stack, Arg.Any<CancellationToken>());
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

        _converter
            .DeserializeStateMachine(s_flowId, ref _heap, s_stateMachine1Bytes, Arg.Is<DTask>(task => task.IsCompleted))
            .Returns(suspendedTask);

        _storage
            .LoadStackAsync(s_flowId, Arg.Any<CancellationToken>())
            .Returns(_stack);

        _stack
            .PopAsync(Arg.Any<CancellationToken>())
            .Returns(s_heapBytes, s_stateMachine1Bytes, s_emptyBytes);

        // Act
        await _sut.ResumeAsync(s_flowId, scope);

        // Assert
        _converter.Received(1).DeserializeHeap(s_flowId, scope, s_heapBytes);
        _ = _converter.Received(1).DeserializeStateMachine(s_flowId, ref _heap, s_stateMachine1Bytes, Arg.Is<DTask>(task => task.IsCompleted));
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

        _converter
            .DeserializeStateMachine(s_flowId, ref _heap, s_stateMachine1Bytes, Arg.Is<DTask>(task => task.IsCompleted))
            .Returns(completedTask);

        _storage
            .LoadStackAsync(s_flowId, Arg.Any<CancellationToken>())
            .Returns(_stack);

        _stack
            .PopAsync(Arg.Any<CancellationToken>())
            .Returns(s_heapBytes, s_stateMachine1Bytes, s_emptyBytes);

        // Act
        await _sut.ResumeAsync(s_flowId, scope);

        // Assert
        _converter.Received(1).DeserializeHeap(s_flowId, scope, s_heapBytes);
        _ = _converter.Received(1).DeserializeStateMachine(s_flowId, ref _heap, s_stateMachine1Bytes, Arg.Is<DTask>(task => task.IsCompleted));
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

        _storage
            .LoadStackAsync(s_flowId, Arg.Any<CancellationToken>())
            .Returns(_stack);

        _converter
            .DeserializeStateMachine(s_flowId, ref _heap, s_stateMachine1Bytes, Arg.Is<DTask>(task => task.IsCompleted))
            .Returns(completedTask);

        _converter
            .DeserializeStateMachine(s_flowId, ref _heap, s_stateMachine2Bytes, completedTask)
            .Returns(completedTask);

        _stack
            .PopAsync(Arg.Any<CancellationToken>())
            .Returns(s_heapBytes, s_stateMachine1Bytes, s_stateMachine2Bytes, s_emptyBytes);

        // Act
        await _sut.ResumeAsync(s_flowId, scope);

        // Assert
        await _stack.Received(4).PopAsync(Arg.Any<CancellationToken>());
    }
}
