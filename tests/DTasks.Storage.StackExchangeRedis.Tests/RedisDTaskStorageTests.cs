using DTasks.Hosting;
using StackExchange.Redis;

namespace DTasks.Storage.StackExchangeRedis;

public class RedisDTaskStorageTests
{
    private static readonly string _flowId = "flowId";
    private static readonly byte[] _stateMachine1Bytes = [1, 2, 3];
    private static readonly byte[] _stateMachine2Bytes = [4, 5, 6];
    private static readonly byte[] _heapBytes = [7, 8, 9];

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
        stack.PushStateMachine(_stateMachine1Bytes);
        stack.PushStateMachine(_stateMachine2Bytes);
        ReadOnlySpan<byte> popped1 = stack.PopStateMachine(out bool hasNext1);
        ReadOnlySpan<byte> popped2 = stack.PopStateMachine(out bool hasNext2);

        // Assert
        popped1.Should().BeEquivalentTo(_stateMachine2Bytes);
        popped2.Should().BeEquivalentTo(_stateMachine1Bytes);
        hasNext1.Should().BeTrue();
        hasNext2.Should().BeFalse();
    }

    [Fact]
    public void PushHeap_Then_PopHeap_ReturnsSameBytes()
    {
        // Arrange
        RedisFlowStack stack = _sut.CreateStack();

        // Act
        stack.PushStateMachine(_stateMachine1Bytes);
        stack.PushHeap(_heapBytes);
        ReadOnlySpan<byte> popped1 = stack.PopHeap();
        ReadOnlySpan<byte> popped2 = stack.PopStateMachine(out _);

        // Assert
        popped1.Should().BeEquivalentTo(_heapBytes);
        popped2.Should().BeEquivalentTo(_stateMachine1Bytes);
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
        stack.PushStateMachine(_stateMachine1Bytes);
        stack.PushHeap(_heapBytes);

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
        stack.PushStateMachine(_stateMachine1Bytes);
        stack.PushHeap(_heapBytes);

        // Act
        Action act = () => stack.PushStateMachine(_stateMachine2Bytes);

        // Assert
        act.Should().ThrowExactly<InvalidOperationException>();
    }

    [Fact]
    public void PushHeap_Throws_WhenNoStateMachinesWerePushed()
    {
        // Arrange
        RedisFlowStack stack = _sut.CreateStack();

        // Act
        Action act = () => stack.PushHeap(_heapBytes);

        // Assert
        act.Should().ThrowExactly<InvalidOperationException>();
    }

    [Fact]
    public void PushHeap_Throws_WhenHeapWasAlreadyPushed()
    {
        // Arrange
        RedisFlowStack stack = _sut.CreateStack();
        stack.PushStateMachine(_stateMachine1Bytes);
        stack.PushHeap(_heapBytes);

        // Act
        Action act = () => stack.PushHeap(_heapBytes);

        // Assert
        act.Should().ThrowExactly<InvalidOperationException>();
    }

    [Fact]
    public void PopHeap_Throws_WhenHeapWasNotPushed()
    {
        // Arrange
        RedisFlowStack stack = _sut.CreateStack();
        stack.PushStateMachine(_stateMachine1Bytes);

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
        await _sut.SaveStackAsync(_flowId, ref stack);

        // Assert
        await _database.Received(1).HashSetAsync(
            key: _flowId,
            hashFields: Arg.Is<HashEntry[]>(entries =>
                entries.Length == 3 &&
                entries[0].Value == _heapBytes &&
                entries[1].Value == _stateMachine2Bytes &&
                entries[2].Value == _stateMachine1Bytes),
            flags: Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task SaveStackAsync_Throws_WhenHeapWasNotPushed()
    {
        // Arrange
        RedisFlowStack stack = _sut.CreateStack();

        // Act
        Func<Task> act = () => _sut.SaveStackAsync(_flowId, ref stack);

        // Assert
        await act.Should().ThrowExactlyAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SaveStackAsync_Throws_WhenCalledMultipleTimesWithSameStack()
    {
        // Arrange
        RedisFlowStack stack = CreateInitializedStack();
        await _sut.SaveStackAsync(_flowId, ref stack);

        // Act
        Func<Task> act = () => _sut.SaveStackAsync(_flowId, ref stack);

        // Assert
        await act.Should().ThrowExactlyAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task LoadStackAsync_RestoresStack()
    {
        // Arrange
        HashEntry[] entries = CreateEntries();
        _database
            .HashGetAllAsync(_flowId, CommandFlags.None)
            .Returns(Task.FromResult(entries));

        // Act
        RedisFlowStack stack = await _sut.LoadStackAsync(_flowId);
        ReadOnlySpan<byte> poppedHeap = stack.PopHeap();
        ReadOnlySpan<byte> popped1 = stack.PopStateMachine(out bool hasNext1);
        ReadOnlySpan<byte> popped2 = stack.PopStateMachine(out bool hasNext2);

        // Assert
        poppedHeap.Should().BeEquivalentTo(_heapBytes);
        popped1.Should().BeEquivalentTo(_stateMachine2Bytes);
        popped2.Should().BeEquivalentTo(_stateMachine1Bytes);
        hasNext1.Should().BeTrue();
        hasNext2.Should().BeFalse();
    }

    [Fact]
    public async Task LoadStackAsync_Throws_EntriesContainsLessThanTwoElements()
    {
        // Arrange
        HashEntry[] entries = CreateEntries();
        entries = [entries[0]];
        _database
            .HashGetAllAsync(_flowId, CommandFlags.None)
            .Returns(Task.FromResult(entries));

        // Act
        Func<Task> act = () => _sut.LoadStackAsync(_flowId);

        // Assert
        (await act.Should().ThrowExactlyAsync<CorruptedDFlowException>()).Which.FlowId.Should().Be(_flowId);
    }

    [Fact]
    public async Task LoadStackAsync_Throws_WhenHeapNameIsInvalid()
    {
        // Arrange
        HashEntry[] entries = CreateEntries();
        entries[0] = new HashEntry(name: "invalid", value: entries[0].Value);
        _database
            .HashGetAllAsync(_flowId, CommandFlags.None)
            .Returns(Task.FromResult(entries));

        // Act
        Func<Task> act = () => _sut.LoadStackAsync(_flowId);

        // Assert
        (await act.Should().ThrowExactlyAsync<CorruptedDFlowException>()).Which.FlowId.Should().Be(_flowId);
    }

    [Fact]
    public async Task LoadStackAsync_Throws_WhenStateMachineNameIsInvalid()
    {
        // Arrange
        HashEntry[] entries = CreateEntries();
        entries[1] = new HashEntry(name: "invalid", value: entries[1].Value);
        _database
            .HashGetAllAsync(_flowId, CommandFlags.None)
            .Returns(Task.FromResult(entries));

        // Act
        Func<Task> act = () => _sut.LoadStackAsync(_flowId);

        // Assert
        (await act.Should().ThrowExactlyAsync<CorruptedDFlowException>()).Which.FlowId.Should().Be(_flowId);
    }

    private RedisFlowStack CreateInitializedStack()
    {
        RedisFlowStack stack = _sut.CreateStack();
        stack.PushStateMachine(_stateMachine1Bytes);
        stack.PushStateMachine(_stateMachine2Bytes);
        stack.PushHeap(_heapBytes);
        return stack;
    }

    private HashEntry[] CreateEntries() => CreateInitializedStack().ToArrayAndDispose();
}
