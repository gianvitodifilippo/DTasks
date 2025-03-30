using DTasks.Infrastructure;
using StackExchange.Redis;

namespace DTasks.Serialization.StackExchangeRedis;

public class RedisDAsyncStorageTests
{
    private readonly IDatabase _database;
    private readonly RedisDAsyncStorage _sut;

    public RedisDAsyncStorageTests()
    {
        _database = Substitute.For<IDatabase>();

        _sut = new RedisDAsyncStorage(_database);
    }

    [Fact]
    public async Task LoadAsync_LoadsStringFromDatabase()
    {
        // Arrange
        DAsyncId id = DAsyncId.New();
        ReadOnlyMemory<byte> expectedBytes = new byte[] { 1, 2, 3 };

        _database.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(expectedBytes);

        // Act
        ReadOnlyMemory<byte> bytes = await _sut.LoadAsync(id);

        // Assert
        bytes.Should().Be(expectedBytes);
    }

    [Fact]
    public async Task SaveAsync_SetsStringInDatabase()
    {
        // Arrange
        DAsyncId id = DAsyncId.New();
        ReadOnlyMemory<byte> bytes = new byte[] { 1, 2, 3 };

        // Act
        await _sut.SaveAsync(id, bytes);

        // Assert
        await _database.Received().StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<bool>(), Arg.Any<When>(), Arg.Any<CommandFlags>());
    }
}
