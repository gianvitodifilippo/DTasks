namespace DTasks.Storage.StackExchangeRedis;

public class RedisFlowStackTests
{
    private static readonly ReadOnlyMemory<byte> s_bytes = new byte[] { 1, 2, 3 };

    [Fact]
    public void Push_PushesMemoryToStack()
    {
        // Arrange
        Stack<ReadOnlyMemory<byte>> items = new();
        RedisFlowStack sut = new RedisFlowStack(items);

        // Act
        sut.Push(s_bytes);

        // Assert
        sut.Items.Should().ContainSingle().Which.Should().Be(s_bytes);
    }

    [Fact]
    public async Task PopAsync_PopsMemoryFromStack()
    {
        // Arrange
        var items = new Stack<ReadOnlyMemory<byte>>();
        items.Push(s_bytes);
        RedisFlowStack sut = new RedisFlowStack(items);

        // Act
        ReadOnlyMemory<byte> result = await sut.PopAsync();

        // Assert
        result.Should().Be(s_bytes);
        sut.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task PopAsync_ReturnsEmptyMemoryWhenStackIsEmpty()
    {
        // Arrange
        Stack<ReadOnlyMemory<byte>> items = new();
        RedisFlowStack sut = new RedisFlowStack(items);

        // Act
        ReadOnlyMemory<byte> result = await sut.PopAsync();

        // Assert
        result.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Push_Throws_WhenDisposed()
    {
        // Arrange
        Stack<ReadOnlyMemory<byte>> items = new();
        RedisFlowStack sut = new RedisFlowStack(items);
        sut.Dispose();

        // Act
        Action act = () => sut.Push(s_bytes);

        // Assert
        act.Should().ThrowExactly<ObjectDisposedException>().Which.ObjectName.Should().Be(nameof(RedisFlowStack));
    }

    [Fact]
    public async Task PopAsync_Throws_WhenDisposed()
    {
        // Arrange
        Stack<ReadOnlyMemory<byte>> items = new();
        RedisFlowStack sut = new RedisFlowStack(items);
        sut.Dispose();

        // Act
        Func<Task> act = async () => await sut.PopAsync();

        // Assert
        (await act.Should().ThrowExactlyAsync<ObjectDisposedException>()).Which.ObjectName.Should().Be(nameof(RedisFlowStack));
    }
}
