#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using DTasks.Marshaling;
using System.Linq.Expressions;

namespace DTasks.Hosting;

public class DAsyncFlowTests
{
    private readonly IDAsyncHost _host;
    private readonly FakeDAsyncStateManager _stateManager;
    private readonly DAsyncFlow _sut;

    public DAsyncFlowTests()
    {
        _host = Substitute.For<IDAsyncHost>();
        _sut = new();
        _stateManager = new(_sut);

        _host
            .CreateStateManager(Arg.Any<IDAsyncMarshaler>())
            .Returns(_stateManager);
    }

    [Fact]
    public async Task RunsCompletedDTask()
    {
        // Arrange
        DTask task = DTask.CompletedDTask;

        // Act
        await _sut.StartAsync(_host, task);

        // Assert
        await _host.Received(1).SucceedAsync();
    }

    [Fact]
    public async Task RunsFromResult()
    {
        // Arrange
        const int result = 42;
        DTask<int> task = DTask.FromResult(result);

        // Act
        await _sut.StartAsync(_host, task);

        // Assert
        await _host.Received(1).SucceedAsync(result);
    }

    [Fact]
    public async Task RunsFromException()
    {
        // Arrange
        Exception exception = new();
        DTask task = DTask.FromException(exception);

        // Act
        await _sut.StartAsync(_host, task);

        // Assert
        await _host.Received(1).FailAsync(exception);
    }

    [Fact]
    public async Task RunsFromExceptionOfResult()
    {
        // Arrange
        Exception exception = new();
        DTask<int> task = DTask<int>.FromException(exception);

        // Act
        await _sut.StartAsync(_host, task);

        // Assert
        await _host.Received(1).FailAsync(exception);
    }

    [Fact]
    public async Task RunsYield()
    {
        // Arrange
        YieldRunnable runnable = new();

        DAsyncId id = default;
        _host
            .When(host => host.YieldAsync(Arg.Any<DAsyncId>()))
            .Do(call => id = call.Arg<DAsyncId>());

        // Act
        await _sut.StartAsync(_host, runnable);
        await _sut.ResumeAsync(_host, id);

        // Assert
        await _host.Received(1).YieldAsync(Arg.Is(NonReservedId));
        await _host.Received(1).SucceedAsync();
        _stateManager.Count.Should().Be(0);
    }

    [Fact]
    public async Task RunsDelay()
    {
        // Arrange
        TimeSpan delay = TimeSpan.FromSeconds(42);
        DTask task = DTask.Delay(delay);

        DAsyncId id = default;
        _host
            .When(host => host.DelayAsync(Arg.Any<DAsyncId>(), Arg.Any<TimeSpan>()))
            .Do(call => id = call.Arg<DAsyncId>());

        // Act
        await _sut.StartAsync(_host, task);
        await _sut.ResumeAsync(_host, id);

        // Assert
        await _host.Received(1).DelayAsync(Arg.Is(NonReservedId), delay);
        await _host.Received(1).SucceedAsync();
        _stateManager.Count.Should().Be(0);
    }

    [Fact]
    public async Task RunsCallback()
    {
        // Arrange
        var callback = Substitute.For<ISuspensionCallback>();
        DTask task = DTask.Factory.Callback(callback);

        DAsyncId id = default;
        callback
            .When(callback => callback.InvokeAsync(Arg.Any<DAsyncId>()))
            .Do(call => id = call.Arg<DAsyncId>());

        // Act
        await _sut.StartAsync(_host, task);
        await _sut.ResumeAsync(_host, id);

        // Assert
        await callback.Received(1).InvokeAsync(Arg.Is(NonReservedId));
        await _host.Received(1).SucceedAsync();
        _stateManager.Count.Should().Be(0);
    }

    [Fact]
    public async Task RunsCallbackOfResult()
    {
        // Arrange
        var callback = Substitute.For<ISuspensionCallback>();
        DTask<int> task = DTask<int>.Factory.Callback(callback);

        DAsyncId id = default;
        callback
            .When(callback => callback.InvokeAsync(Arg.Any<DAsyncId>()))
            .Do(call => id = call.Arg<DAsyncId>());

        // Act
        await _sut.StartAsync(_host, task);
        await _sut.ResumeAsync(_host, id);

        // Assert
        await callback.Received(1).InvokeAsync(Arg.Is(NonReservedId));
        await _host.Received(1).SucceedAsync();
        _stateManager.Count.Should().Be(0);
    }

    [Fact]
    public async Task RunsCallbackWithState()
    {
        // Arrange
        var state = new object();
        var callback = Substitute.For<ISuspensionCallback<object>>();
        DTask task = DTask.Factory.Callback(state, callback);

        DAsyncId id = default;
        callback
            .When(callback => callback.InvokeAsync(Arg.Any<DAsyncId>(), Arg.Any<object>()))
            .Do(call => id = call.Arg<DAsyncId>());

        // Act
        await _sut.StartAsync(_host, task);
        await _sut.ResumeAsync(_host, id);

        // Assert
        await callback.Received(1).InvokeAsync(Arg.Is(NonReservedId), state);
        await _host.Received(1).SucceedAsync();
        _stateManager.Count.Should().Be(0);
    }

    [Fact]
    public async Task RunsCallbackOfResultWithState()
    {
        // Arrange
        var state = new object();
        var callback = Substitute.For<ISuspensionCallback<object>>();
        DTask<int> task = DTask<int>.Factory.Callback(state, callback);

        DAsyncId id = default;
        callback
            .When(callback => callback.InvokeAsync(Arg.Any<DAsyncId>(), Arg.Any<object>()))
            .Do(call => id = call.Arg<DAsyncId>());

        // Act
        await _sut.StartAsync(_host, task);
        await _sut.ResumeAsync(_host, id);

        // Assert
        await callback.Received(1).InvokeAsync(Arg.Is(NonReservedId), state);
        await _host.Received(1).SucceedAsync();
        _stateManager.Count.Should().Be(0);
    }

    [Fact]
    public async Task RunsDelegateCallback()
    {
        // Arrange
        var callback = Substitute.For<SuspensionCallback>();
        DTask task = DTask.Factory.Callback(callback);

        DAsyncId id = default;
        callback
            .When(callback => callback.Invoke(Arg.Any<DAsyncId>()))
            .Do(call => id = call.Arg<DAsyncId>());

        // Act
        await _sut.StartAsync(_host, task);
        await _sut.ResumeAsync(_host, id);

        // Assert
        await callback.Received(1).Invoke(Arg.Is(NonReservedId));
        await _host.Received(1).SucceedAsync();
        _stateManager.Count.Should().Be(0);
    }

    [Fact]
    public async Task RunsDelegateCallbackOfResult()
    {
        // Arrange
        var callback = Substitute.For<SuspensionCallback>();
        DTask<int> task = DTask<int>.Factory.Callback(callback);

        DAsyncId id = default;
        callback
            .When(callback => callback.Invoke(Arg.Any<DAsyncId>()))
            .Do(call => id = call.Arg<DAsyncId>());

        // Act
        await _sut.StartAsync(_host, task);
        await _sut.ResumeAsync(_host, id);

        // Assert
        await callback.Received(1).Invoke(Arg.Is(NonReservedId));
        await _host.Received(1).SucceedAsync();
        _stateManager.Count.Should().Be(0);
    }

    [Fact]
    public async Task RunsDelegateCallbackWithState()
    {
        // Arrange
        var state = new object();
        var callback = Substitute.For<SuspensionCallback<object>>();
        DTask task = DTask.Factory.Callback(state, callback);

        DAsyncId id = default;
        callback
            .When(callback => callback.Invoke(Arg.Any<DAsyncId>(), Arg.Any<object>()))
            .Do(call => id = call.Arg<DAsyncId>());

        // Act
        await _sut.StartAsync(_host, task);
        await _sut.ResumeAsync(_host, id);

        // Assert
        await callback.Received(1).Invoke(Arg.Is(NonReservedId), state);
        await _host.Received(1).SucceedAsync();
        _stateManager.Count.Should().Be(0);
    }

    [Fact]
    public async Task RunsDelegateCallbackOfResultWithState()
    {
        // Arrange
        var state = new object();
        var callback = Substitute.For<SuspensionCallback<object>>();
        DTask<int> task = DTask<int>.Factory.Callback(state, callback);

        DAsyncId id = default;
        callback
            .When(callback => callback.Invoke(Arg.Any<DAsyncId>(), Arg.Any<object>()))
            .Do(call => id = call.Arg<DAsyncId>());

        // Act
        await _sut.StartAsync(_host, task);
        await _sut.ResumeAsync(_host, id);

        // Assert
        await callback.Received(1).Invoke(Arg.Is(NonReservedId), state);
        await _host.Received(1).SucceedAsync();
        _stateManager.Count.Should().Be(0);
    }

    [Fact]
    public async Task RunsDTaskThatCompletesLocallyAndSynchronously()
    {
        // Arrange
        const int result = 42;
        static async DTask<int> M1()
        {
            return result;
        }

        DTask<int> task = M1();

        // Act
        await _sut.StartAsync(_host, task);

        // Assert
        await _host.Received(1).SucceedAsync(result);
        _stateManager.Count.Should().Be(0);
    }

    [Fact]
    public async Task RunsDTaskThatCompletesLocallyAndAsynchronously()
    {
        // Arrange
        const int result = 42;
        static async DTask<int> M1()
        {
            await Task.Delay(100);
            return result;
        }

        DTask<int> task = M1();

        // Act
        await _sut.StartAsync(_host, task);

        // Assert
        await _host.Received(1).SucceedAsync(result);
        _stateManager.Count.Should().Be(0);
    }

    [Fact]
    public async Task RunsDTaskThatAwaitsCompletedDTask()
    {
        // Arrange
        const int result = 42;
        static async DTask<int> M1()
        {
            await DTask.CompletedDTask;
            return result;
        }

        DTask<int> task = M1();

        // Act
        await _sut.StartAsync(_host, task);

        // Assert
        await _host.Received(1).SucceedAsync(result);
        _stateManager.Count.Should().Be(0);
    }

    [Fact]
    public async Task RunsDTaskThatAwaitsLocallyCompletingDTask()
    {
        // Arrange
        const int result = 42;
        static async DTask<int> M1()
        {
            int result = await M2();
            return result + 1;
        }

        static async DTask<int> M2()
        {
            return result;
        }

        DTask<int> task = M1();

        // Act
        await _sut.StartAsync(_host, task);

        // Assert
        await _host.Received(1).SucceedAsync(result + 1);
        _stateManager.Count.Should().Be(0);
    }

    private static Expression<Predicate<DAsyncId>> NonReservedId => id => id != default && id != DAsyncId.RootId;

    private sealed class YieldRunnable : IDAsyncRunnable
    {
        public void Run(IDAsyncFlow flow) => DTask.Yield().GetAwaiter().Continue(flow);
    }
}
