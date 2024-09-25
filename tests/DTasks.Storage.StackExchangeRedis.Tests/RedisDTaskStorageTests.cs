using DTasks.Hosting;
using StackExchange.Redis;

namespace DTasks.Storage.StackExchangeRedis;

public class RedisDTaskStorageTests
{
    private static readonly string s_flowId = "flowId";
    private static readonly byte[] s_stateMachine1Bytes = [1, 2, 3];
    private static readonly byte[] s_stateMachine2Bytes = [4, 5, 6];
    private static readonly byte[] s_heapBytes = [7, 8, 9];

    private readonly IDatabase _database;
    private readonly RedisDTaskStorage _sut;

    public RedisDTaskStorageTests()
    {
        _database = Substitute.For<IDatabase>();
        _sut = new RedisDTaskStorage(_database);
    }

    [Fact]
    public void PushStateMachine_Then_PopStateMachine_ReturnsSameBytes()
    {
        // Arrange
        RedisFlowStack stack = _sut.CreateStack();

        // Act
        stack.PushStateMachine(s_stateMachine1Bytes);
        stack.PushStateMachine(s_stateMachine2Bytes);
        ReadOnlySpan<byte> popped1 = stack.PopStateMachine(out bool hasNext1);
        ReadOnlySpan<byte> popped2 = stack.PopStateMachine(out bool hasNext2);

        // Assert
        popped1.Should().BeEquivalentTo(s_stateMachine2Bytes);
        popped2.Should().BeEquivalentTo(s_stateMachine1Bytes);
        hasNext1.Should().BeTrue();
        hasNext2.Should().BeFalse();
    }

    [Fact]
    public void PushHeap_Then_PopHeap_ReturnsSameBytes()
    {
        // Arrange
        RedisFlowStack stack = _sut.CreateStack();

        // Act
        stack.PushStateMachine(s_stateMachine1Bytes);
        stack.PushHeap(s_heapBytes);
        ReadOnlySpan<byte> popped1 = stack.PopHeap();
        ReadOnlySpan<byte> popped2 = stack.PopStateMachine(out _);

        // Assert
        popped1.Should().BeEquivalentTo(s_heapBytes);
        popped2.Should().BeEquivalentTo(s_stateMachine1Bytes);
    }

    [Fact]
    public void PopStateMachine_Throws_WhenNoStateMachinesWerePushed()
    {
        // Arrange
        RedisFlowStack stack = _sut.CreateStack();

        // Act
        Action act = () => stack.PopStateMachine(out _);

        // Assert
        act.Should().ThrowExactly<InvalidOperationException>();
    }

    [Fact]
    public void PopStateMachine_Throws_WhenHeapWasPushed()
    {
        // Arrange
        RedisFlowStack stack = _sut.CreateStack();
        stack.PushStateMachine(s_stateMachine1Bytes);
        stack.PushHeap(s_heapBytes);

        // Act
        Action act = () => stack.PopStateMachine(out _);

        // Assert
        act.Should().ThrowExactly<InvalidOperationException>();
    }

    [Fact]
    public void PushStateMachine_Throws_WhenHeapWasPushed()
    {
        // Arrange
        RedisFlowStack stack = _sut.CreateStack();
        stack.PushStateMachine(s_stateMachine1Bytes);
        stack.PushHeap(s_heapBytes);

        // Act
        Action act = () => stack.PushStateMachine(s_stateMachine2Bytes);

        // Assert
        act.Should().ThrowExactly<InvalidOperationException>();
    }

    [Fact]
    public void PushHeap_Throws_WhenNoStateMachinesWerePushed()
    {
        // Arrange
        RedisFlowStack stack = _sut.CreateStack();

        // Act
        Action act = () => stack.PushHeap(s_heapBytes);

        // Assert
        act.Should().ThrowExactly<InvalidOperationException>();
    }

    [Fact]
    public void PushHeap_Throws_WhenHeapWasAlreadyPushed()
    {
        // Arrange
        RedisFlowStack stack = _sut.CreateStack();
        stack.PushStateMachine(s_stateMachine1Bytes);
        stack.PushHeap(s_heapBytes);

        // Act
        Action act = () => stack.PushHeap(s_heapBytes);

        // Assert
        act.Should().ThrowExactly<InvalidOperationException>();
    }

    [Fact]
    public void PopHeap_Throws_WhenHeapWasNotPushed()
    {
        // Arrange
        RedisFlowStack stack = _sut.CreateStack();
        stack.PushStateMachine(s_stateMachine1Bytes);

        // Act
        Action act = () => stack.PopHeap();

        // Assert
        act.Should().ThrowExactly<InvalidOperationException>();
    }

    [Fact]
    public async Task SaveStackAsync_SavesHeapThenStateMachinesInOrder()
    {
        // Arrange
        RedisFlowStack stack = CreateInitializedStack();

        // Act
        await _sut.SaveStackAsync(s_flowId, ref stack);

        // Assert
        await _database.Received(1).ListRightPushAsync(
            key: s_flowId,
            values: Arg.Is<RedisValue[]>(items =>
                items.Length == 3 &&
                items[0] == s_stateMachine1Bytes &&
                items[1] == s_stateMachine2Bytes &&
                items[2] == s_heapBytes),
            when: When.Always,
            flags: Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task SaveStackAsync_Throws_WhenHeapWasNotPushed()
    {
        // Arrange
        RedisFlowStack stack = _sut.CreateStack();

        // Act
        Func<Task> act = () => _sut.SaveStackAsync(s_flowId, ref stack);

        // Assert
        await act.Should().ThrowExactlyAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SaveStackAsync_Throws_WhenCalledMultipleTimesWithSameStack()
    {
        // Arrange
        RedisFlowStack stack = CreateInitializedStack();
        await _sut.SaveStackAsync(s_flowId, ref stack);

        // Act
        Func<Task> act = () => _sut.SaveStackAsync(s_flowId, ref stack);

        // Assert
        await act.Should().ThrowExactlyAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task LoadStackAsync_RestoresStack()
    {
        // Arrange
        RedisValue[] items = CreateItems();
        _database
            .ListRangeAsync(s_flowId)
            .Returns(Task.FromResult(items));

        // Act
        RedisFlowStack stack = await _sut.LoadStackAsync(s_flowId);
        ReadOnlySpan<byte> poppedHeap = stack.PopHeap();
        ReadOnlySpan<byte> popped1 = stack.PopStateMachine(out bool hasNext1);
        ReadOnlySpan<byte> popped2 = stack.PopStateMachine(out bool hasNext2);

        // Assert
        poppedHeap.Should().BeEquivalentTo(s_heapBytes);
        popped1.Should().BeEquivalentTo(s_stateMachine2Bytes);
        popped2.Should().BeEquivalentTo(s_stateMachine1Bytes);
        hasNext1.Should().BeTrue();
        hasNext2.Should().BeFalse();
    }

    [Fact]
    public async Task LoadStackAsync_Throws_EntriesContainsLessThanTwoElements()
    {
        // Arrange
        RedisValue[] items = CreateItems();
        items = [items[0]];
        _database
            .ListRangeAsync(s_flowId)
            .Returns(Task.FromResult(items));

        // Act
        Func<Task> act = () => _sut.LoadStackAsync(s_flowId);

        // Assert
        (await act.Should().ThrowExactlyAsync<CorruptedDFlowException>()).Which.FlowId.Should().Be(s_flowId);
    }

    private RedisFlowStack CreateInitializedStack()
    {
        RedisFlowStack stack = _sut.CreateStack();
        stack.PushStateMachine(s_stateMachine1Bytes);
        stack.PushStateMachine(s_stateMachine2Bytes);
        stack.PushHeap(s_heapBytes);
        return stack;
    }

    private RedisValue[] CreateItems() => CreateInitializedStack().ToArrayAndDispose();
}
