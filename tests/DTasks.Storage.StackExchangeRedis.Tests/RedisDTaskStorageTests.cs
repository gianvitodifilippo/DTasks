using DTasks.Hosting;
using StackExchange.Redis;

namespace DTasks.Storage.StackExchangeRedis;

public class RedisDTaskStorageTests
{
    private static readonly ReadOnlyMemory<byte> s_bytes1 = new byte[] { 1, 2, 3 };
    private static readonly ReadOnlyMemory<byte> s_bytes2 = new byte[] { 4, 5, 6 };

    private readonly IDatabase _database;
    private readonly RedisDTaskStorage _sut;

    public RedisDTaskStorageTests()
    {
        _database = Substitute.For<IDatabase>();
        _sut = new RedisDTaskStorage(_database);
    }

    [Fact]
    public void CreateStack_ShouldReturnEmptyStack()
    {
        // Arrange

        // Act
        RedisFlowStack stack = _sut.CreateStack();

        // Assert
        stack.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadStackAsync_ShouldReturnStackWithItemsFromDatabase()
    {
        // Arrange
        var flowId = FlowId.New(FlowKind.Hosted);

        _database
            .ListRangeAsync(flowId.ToString())
            .Returns([s_bytes1, s_bytes2]);

        // Act
        RedisFlowStack stack = await _sut.LoadStackAsync(flowId);

        // Assert
        stack.Items.Should().HaveCount(2).And.ContainInConsecutiveOrder(s_bytes2, s_bytes1);
    }

    [Fact]
    public async Task SaveStackAsync_ShouldSaveStackToDatabaseAndClearItems()
    {
        // Arrange
        var flowId = FlowId.New(FlowKind.Hosted);
        Stack<ReadOnlyMemory<byte>> items = new([s_bytes1, s_bytes2]);
        RedisFlowStack stack = new(items);

        // Act
        await _sut.SaveStackAsync(flowId, ref stack);

        // Assert
        await _database.Received(1).ListLeftPushAsync(flowId.ToString(), Arg.Is<RedisValue[]>(values => values.SequenceEqual(new RedisValue[] { s_bytes2, s_bytes1 })), When.Always, Arg.Any<CommandFlags>());
        stack.Items.Should().BeEmpty();
    }
}
