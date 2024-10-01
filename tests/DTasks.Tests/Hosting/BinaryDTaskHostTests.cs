using System.Text;
using static DTasks.Hosting.HostingFixtures;

namespace DTasks.Hosting;

public class BinaryDTaskHostTests
{
    private static readonly byte[] s_heapBytes = Encoding.UTF8.GetBytes("heap");
    private static readonly byte[] s_stateMachine1Bytes = Encoding.UTF8.GetBytes("SM1");
    private static readonly byte[] s_stateMachine2Bytes = Encoding.UTF8.GetBytes("SM2");
    private static readonly byte[] s_contextBytes = Encoding.UTF8.GetBytes("context");
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
            .SerializeHeap(ref _heap)
            .Returns(s_heapBytes);

        _converter
            .DeserializeHeap(Arg.Any<IDTaskScope>(), s_heapBytes)
            .Returns(_heap);

        _storage
            .CreateStack()
            .Returns(_stack);

        _sut = Substitute.For<TestBinaryDTaskHost>(_storage, _converter);
    }

    [Fact]
    public async Task SuspendAsync_ShouldSaveContextAndStackAndHeap()
    {
        // Arrange
        var scope = Substitute.For<IDTaskScope>();
        var suspendedTask = Substitute.For<TestSuspendedDTask>(true);
        var context = new TestFlowContext();

        _converter
            .Serialize(context)
            .Returns(s_contextBytes);

        _converter
            .SerializeStateMachine(ref _heap, ref Arg.Any<Arg.AnyType>(), Arg.Any<IStateMachineInfo>())
            .Returns(s_stateMachine1Bytes);

        _heap.StackCount.Returns(1u);

        // Act
        await _sut.SuspendAsync(context, scope, suspendedTask.GetDAwaiter());

        // Assert
        _converter.Received(1).CreateHeap(scope);
        _converter.Received(1).SerializeHeap(ref _heap);
        _storage.Received(1).CreateStack();
        _stack.Received(1).Push(s_contextBytes);
        _stack.Received(1).Push(s_stateMachine1Bytes);
        _stack.Received(1).Push(s_heapBytes);
        await _storage.Received(1).SaveStackAsync(Arg.Any<FlowId>(), ref _stack, Arg.Any<CancellationToken>());
        suspendedTask.Received().SaveState_Public(Arg.Any<IStateHandler>());
        await suspendedTask.Received().SuspendAsync_Public(Arg.Any<ISuspensionHandler>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResumeAsync_ShouldSuspend_WhenFlowIsSuspended()
    {
        // Arrange
        var suspendedTask = Substitute.For<TestSuspendedDTask>(true);
        var scope = Substitute.For<IDTaskScope>();
        var flowId = FlowId.New(FlowKind.Hosted);

        _converter
            .DeserializeStateMachine(ref _heap, s_stateMachine1Bytes, Arg.Is<DTask>(task => task.IsCompleted))
            .Returns(suspendedTask);

        _converter
            .SerializeStateMachine(ref _heap, ref Arg.Any<Arg.AnyType>(), Arg.Any<IStateMachineInfo>())
            .Returns(s_stateMachine1Bytes);

        _converter
            .DeserializeHeap(scope, s_heapBytes)
            .Returns(_heap);

        _storage
            .LoadStackAsync(flowId, Arg.Any<CancellationToken>())
            .Returns(_stack);

        _stack
            .PopAsync(Arg.Any<CancellationToken>())
            .Returns(s_heapBytes, s_stateMachine1Bytes, s_contextBytes, s_emptyBytes);

        _heap.StackCount.Returns(1u);

        // Act
        await _sut.ResumeAsync(flowId, scope);

        // Assert
        _converter.Received(1).DeserializeHeap(scope, s_heapBytes);
        _ = _converter.Received(1).DeserializeStateMachine(ref _heap, s_stateMachine1Bytes, Arg.Is<DTask>(task => task.IsCompleted));
        suspendedTask.Received().SaveState_Public(Arg.Any<IStateHandler>());
        await suspendedTask.Received().SuspendAsync_Public(Arg.Any<ISuspensionHandler>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResumeAsync_ShouldComplete_WhenFlowIsCompleted()
    {
        // Arrange
        var completedTask = Substitute.For<TestCompletedDTask>();
        var scope = Substitute.For<IDTaskScope>();
        var flowId = FlowId.New(FlowKind.Hosted);

        _converter
            .DeserializeStateMachine(ref _heap, s_stateMachine1Bytes, Arg.Is<DTask>(task => task.IsCompleted))
            .Returns(completedTask);

        _converter
            .DeserializeHeap(scope, s_heapBytes)
            .Returns(_heap);

        _storage
            .LoadStackAsync(flowId, Arg.Any<CancellationToken>())
            .Returns(_stack);

        _stack
            .PopAsync(Arg.Any<CancellationToken>())
            .Returns(s_heapBytes, s_stateMachine1Bytes, s_contextBytes, s_emptyBytes);

        _heap.StackCount.Returns(1u);

        // Act
        await _sut.ResumeAsync(flowId, scope);

        // Assert
        _converter.Received(1).DeserializeHeap(scope, s_heapBytes);
        _converter.Received(1).Deserialize<TestFlowContext>(s_contextBytes);
        _ = _converter.Received(1).DeserializeStateMachine(ref _heap, s_stateMachine1Bytes, Arg.Is<DTask>(task => task.IsCompleted));
        await _storage.Received(1).ClearStackAsync(flowId, ref _stack, Arg.Any<CancellationToken>());
        await completedTask.Received().CompleteAsync_Public(Arg.Any<ICompletionHandler>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResumeAsync_ShouldMoveNext_WhenStackContainsCompletedTask()
    {
        // Arrange
        var completedTask = Substitute.For<TestCompletedDTask>();
        var scope = Substitute.For<IDTaskScope>();
        var flowId = FlowId.New(FlowKind.Hosted);

        _storage
            .LoadStackAsync(flowId, Arg.Any<CancellationToken>())
            .Returns(_stack);

        _converter
            .DeserializeStateMachine(ref _heap, s_stateMachine1Bytes, Arg.Is<DTask>(task => task.IsCompleted))
            .Returns(completedTask);

        _converter
            .DeserializeHeap(scope, s_heapBytes)
            .Returns(_heap);

        _converter
            .DeserializeStateMachine(ref _heap, s_stateMachine2Bytes, completedTask)
            .Returns(completedTask);

        _stack
            .PopAsync(Arg.Any<CancellationToken>())
            .Returns(s_heapBytes, s_stateMachine1Bytes, s_stateMachine2Bytes, s_contextBytes, s_emptyBytes);

        _heap.StackCount.Returns(2u);

        // Act
        await _sut.ResumeAsync(flowId, scope);

        // Assert
        await _stack.Received(4).PopAsync(Arg.Any<CancellationToken>());
    }
}
