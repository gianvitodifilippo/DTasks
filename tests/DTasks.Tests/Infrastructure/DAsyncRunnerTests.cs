#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using System.Linq.Expressions;
using DTasks.Configuration;
using DTasks.Configuration.DependencyInjection;
using DTasks.Execution;
using DTasks.Infrastructure.Execution;
using DTasks.Infrastructure.Fakes;
using DTasks.Infrastructure.Marshaling;
using NSubstitute.ExceptionExtensions;
using Xunit.Sdk;

namespace DTasks.Infrastructure;

// TODO: Verify what cancellation token is passed to the infrastructure
public class DAsyncRunnerTests
{
    private readonly FakeStorage _storage;
    private readonly IDAsyncSuspensionHandler _suspensionHandler;
    private readonly IDAsyncHost _host;
    private readonly DAsyncRunner _sut;

    public DAsyncRunnerTests()
    {
        _storage = new();
        _suspensionHandler = Substitute.For<IDAsyncSuspensionHandler>();

        var stateManagerDescriptor =
            from flow in ComponentDescriptors.Flow
            select new FakeDAsyncStateManager(_storage, flow.Configuration.TypeResolver, flow.Surrogator);
        
        DTasksConfiguration configuration = DTasksConfiguration.Create(configuration => configuration
            .ConfigureState(state => state
                .UseStack(stateManagerDescriptor)
                .UseHeap(stateManagerDescriptor))
            .ConfigureExecution(execution => execution
                .UseSuspensionHandler(ComponentDescriptor.Singleton(_suspensionHandler))));

        _host = Substitute.For<IDAsyncHost>();
        _host.Configuration.Returns(configuration);
        
        _sut = DAsyncRunner.Create();
    }

    [Fact]
    public async Task RunsCompletedDTask()
    {
        // Arrange
        DTask task = DTask.CompletedDTask;

        // Act
        await _sut.StartAsync(_host, task);

        // Assert
        await _host.Received(1).OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), Arg.Any<CancellationToken>());
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
        await _host.Received(1).OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), result, Arg.Any<CancellationToken>());
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
        await _host.Received(1).OnFailAsync(Arg.Any<IDAsyncFlowCompletionContext>(), exception, Arg.Any<CancellationToken>());
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
        await _host.Received(1).OnFailAsync(Arg.Any<IDAsyncFlowCompletionContext>(), exception, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunsYield()
    {
        // Arrange
        YieldRunnable runnable = new();

        DAsyncId id = default;
        _suspensionHandler
            .When(handler => handler.OnYieldAsync(Arg.Any<DAsyncId>(), Arg.Any<CancellationToken>()))
            .Do(call => id = call.Arg<DAsyncId>());

        // Act
        await _sut.StartAsync(_host, runnable);
        await _sut.ResumeAsync(_host, id);

        // Assert
        await _suspensionHandler.Received(1).OnYieldAsync(Arg.Is(NonReservedId), Arg.Any<CancellationToken>());
        await _host.Received(1).OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunsDelay()
    {
        // Arrange
        TimeSpan delay = TimeSpan.FromSeconds(42);
        DTask task = DTask.Delay(delay);

        DAsyncId id = default;
        _suspensionHandler
            .When(handler =>
                handler.OnDelayAsync(Arg.Any<DAsyncId>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()))
            .Do(call => id = call.Arg<DAsyncId>());

        // Act
        await _sut.StartAsync(_host, task);
        await _sut.ResumeAsync(_host, id);

        // Assert
        await _suspensionHandler.Received(1)
            .OnDelayAsync(Arg.Is(NonReservedId), delay, Arg.Any<CancellationToken>());
        await _host.Received(1).OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunsCallback()
    {
        // Arrange
        var callback = Substitute.For<ISuspensionCallback>();
        DTask task = DTask.Factory.Callback(callback);

        DAsyncId id = default;
        callback
            .When(cb => cb.InvokeAsync(Arg.Any<DAsyncId>()))
            .Do(call => id = call.Arg<DAsyncId>());

        // Act
        await _sut.StartAsync(_host, task);
        await _sut.ResumeAsync(_host, id);

        // Assert
        await callback.Received(1).InvokeAsync(Arg.Is(NonReservedId));
        await _host.Received(1).OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunsCallbackOfResult()
    {
        // Arrange
        var callback = Substitute.For<ISuspensionCallback>();
        DTask<int> task = DTask<int>.Factory.Callback(callback);

        DAsyncId id = default;
        callback
            .When(cb => cb.InvokeAsync(Arg.Any<DAsyncId>()))
            .Do(call => id = call.Arg<DAsyncId>());

        // Act
        await _sut.StartAsync(_host, task);
        await _sut.ResumeAsync(_host, id);

        // Assert
        await callback.Received(1).InvokeAsync(Arg.Is(NonReservedId));
        await _host.Received(1).OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), Arg.Any<CancellationToken>());
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
            .When(cb => cb.InvokeAsync(Arg.Any<DAsyncId>(), Arg.Any<object>()))
            .Do(call => id = call.Arg<DAsyncId>());

        // Act
        await _sut.StartAsync(_host, task);
        await _sut.ResumeAsync(_host, id);

        // Assert
        await callback.Received(1).InvokeAsync(Arg.Is(NonReservedId), state);
        await _host.Received(1).OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), Arg.Any<CancellationToken>());
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
            .When(cb => cb.InvokeAsync(Arg.Any<DAsyncId>(), Arg.Any<object>()))
            .Do(call => id = call.Arg<DAsyncId>());

        // Act
        await _sut.StartAsync(_host, task);
        await _sut.ResumeAsync(_host, id);

        // Assert
        await callback.Received(1).InvokeAsync(Arg.Is(NonReservedId), state);
        await _host.Received(1).OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunsDelegateCallback()
    {
        // Arrange
        var callback = Substitute.For<SuspensionCallback>();
        DTask task = DTask.Factory.Callback(callback);

        DAsyncId id = default;
        callback
            .When(cb => cb.Invoke(Arg.Any<DAsyncId>()))
            .Do(call => id = call.Arg<DAsyncId>());

        // Act
        await _sut.StartAsync(_host, task);
        await _sut.ResumeAsync(_host, id);

        // Assert
        await callback.Received(1).Invoke(Arg.Is(NonReservedId));
        await _host.Received(1).OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunsDelegateCallbackOfResult()
    {
        // Arrange
        var callback = Substitute.For<SuspensionCallback>();
        DTask<int> task = DTask<int>.Factory.Callback(callback);

        DAsyncId id = default;
        callback
            .When(cb => cb.Invoke(Arg.Any<DAsyncId>()))
            .Do(call => id = call.Arg<DAsyncId>());

        // Act
        await _sut.StartAsync(_host, task);
        await _sut.ResumeAsync(_host, id);

        // Assert
        await callback.Received(1).Invoke(Arg.Is(NonReservedId));
        await _host.Received(1).OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), Arg.Any<CancellationToken>());
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
            .When(cb => cb.Invoke(Arg.Any<DAsyncId>(), Arg.Any<object>()))
            .Do(call => id = call.Arg<DAsyncId>());

        // Act
        await _sut.StartAsync(_host, task);
        await _sut.ResumeAsync(_host, id);

        // Assert
        await callback.Received(1).Invoke(Arg.Is(NonReservedId), state);
        await _host.Received(1).OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), Arg.Any<CancellationToken>());
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
            .When(cb => cb.Invoke(Arg.Any<DAsyncId>(), Arg.Any<object>()))
            .Do(call => id = call.Arg<DAsyncId>());

        // Act
        await _sut.StartAsync(_host, task);
        await _sut.ResumeAsync(_host, id);

        // Assert
        await callback.Received(1).Invoke(Arg.Is(NonReservedId), state);
        await _host.Received(1).OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), Arg.Any<CancellationToken>());
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
        await _host.Received(1).OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), result, Arg.Any<CancellationToken>());
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
        await _host.Received(1).OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), result, Arg.Any<CancellationToken>());
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
        await _host.Received(1).OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), result, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunsDTaskThatAwaitsResumingDTask()
    {
        // Arrange
        static async DTask M1()
        {
            await new ResumingDTask();
        }

        DTask task = M1();

        // Act
        await _sut.StartAsync(_host, task);

        // Assert
        await _host.Received(1).OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunsDTaskThatAwaitsResumingDTaskOfResult()
    {
        // Arrange
        const int result = 42;

        static async DTask<int> M1()
        {
            return await new ResumingDTask<int>(result);
        }

        DTask<int> task = M1();

        // Act
        await _sut.StartAsync(_host, task);

        // Assert
        await _host.Received(1).OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), result, Arg.Any<CancellationToken>());
    }

    [Fact(Skip = "Not implemented yet")]
    public async Task RunsDTaskThatAwaitsExceptionResumingDTask()
    {
        // Arrange
        Exception exception = new();

        static async DTask M1(Exception exception)
        {
            await new ExceptionResumingDTask(exception);
        }

        DTask task = M1(exception);

        // Act
        await _sut.StartAsync(_host, task);

        // Assert
        await _host.Received(1).OnFailAsync(Arg.Any<IDAsyncFlowCompletionContext>(), exception, Arg.Any<CancellationToken>());
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
        await _host.Received(1).OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), result + 1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunsDTaskThatAwaitsYield()
    {
        // Arrange
        const int result = 42;

        static async DTask<int> M1()
        {
            await DTask.Yield();
            return result;
        }

        DAsyncId id = default;
        _suspensionHandler
            .When(handler => handler.OnYieldAsync(Arg.Any<DAsyncId>(), Arg.Any<CancellationToken>()))
            .Do(call => id = call.Arg<DAsyncId>());

        DTask task = M1();

        // Act
        await _sut.StartAsync(_host, task);
        await _sut.ResumeAsync(_host, id);

        // Assert
        await _suspensionHandler.Received(1).OnYieldAsync(Arg.Is(NonReservedId), Arg.Any<CancellationToken>());
        await _host.Received(1).OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), result, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunsDTaskThatAwaitsDelay()
    {
        // Arrange
        const int result = 42;
        TimeSpan delay = TimeSpan.FromMinutes(1);

        static async DTask<int> M1(TimeSpan delay)
        {
            await DTask.Delay(delay);
            return result;
        }

        DAsyncId id = default;
        _suspensionHandler
            .When(handler =>
                handler.OnDelayAsync(Arg.Any<DAsyncId>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()))
            .Do(call => id = call.Arg<DAsyncId>());

        DTask task = M1(delay);

        // Act
        await _sut.StartAsync(_host, task);
        await _sut.ResumeAsync(_host, id);

        // Assert
        await _suspensionHandler.Received(1)
            .OnDelayAsync(Arg.Any<DAsyncId>(), delay, Arg.Any<CancellationToken>());
        await _host.Received(1).OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), result, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunsDTaskThatAwaitsCallback()
    {
        // Arrange
        const int result = 42;
        var callback = Substitute.For<ISuspensionCallback>();

        static async DTask<int> M1(ISuspensionCallback callback)
        {
            await DTask.Factory.Callback(callback);
            return result;
        }

        DAsyncId id = default;
        callback
            .When(cb => cb.InvokeAsync(Arg.Any<DAsyncId>()))
            .Do(call => id = call.Arg<DAsyncId>());

        DTask task = M1(callback);

        // Act
        await _sut.StartAsync(_host, task);
        await _sut.ResumeAsync(_host, id);

        // Assert
        await callback.Received(1).InvokeAsync(Arg.Any<DAsyncId>());
        await _host.Received(1).OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), result, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunsDTaskThatAwaitsCallbackOfResult()
    {
        // Arrange
        const int result = 42;
        var callback = Substitute.For<ISuspensionCallback>();

        static async DTask<int> M1(ISuspensionCallback callback)
        {
            return await DTask<int>.Factory.Callback(callback);
        }

        DAsyncId id = default;
        callback
            .When(cb => cb.InvokeAsync(Arg.Any<DAsyncId>()))
            .Do(call => id = call.Arg<DAsyncId>());

        DTask task = M1(callback);

        // Act
        await _sut.StartAsync(_host, task);
        await _sut.ResumeAsync(_host, id, result);

        // Assert
        await callback.Received(1).InvokeAsync(Arg.Any<DAsyncId>());
        await _host.Received(1).OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), result, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task YieldingTwiceSavesStateWithSameIdWithoutExposingIt()
    {
        // Arrange
        static async DTask M1()
        {
            await DTask.Yield();
            await DTask.Yield();
        }

        DAsyncId hostYieldId1 = default;
        DAsyncId hostYieldId2 = default;

        _suspensionHandler
            .When(handler => handler.OnYieldAsync(Arg.Any<DAsyncId>(), Arg.Any<CancellationToken>()))
            .Do(call =>
            {
                if (hostYieldId1 == default)
                {
                    hostYieldId1 = call.Arg<DAsyncId>();
                    return;
                }

                if (hostYieldId2 == default)
                {
                    hostYieldId2 = call.Arg<DAsyncId>();
                    return;
                }

                throw FailException.ForFailure("YieldAsync called too many times");
            });

        DTask task = M1();

        // Act
        await _sut.StartAsync(_host, task);
        await _sut.ResumeAsync(_host, hostYieldId1);
        await _sut.ResumeAsync(_host, hostYieldId2);

        // Assert
        DAsyncId id1 = _storage.Ids[0];
        DAsyncId yieldId1 = _storage.Ids[1];
        DAsyncId id2 = _storage.Ids[2];
        DAsyncId yieldId2 = _storage.Ids[3];
        
        hostYieldId1.Should().Be(yieldId1);
        hostYieldId2.Should().Be(yieldId2);
        hostYieldId1.Should().NotBe(hostYieldId2);
        id1.Should().Be(id2).And.NotBe(hostYieldId1).And.NotBe(hostYieldId2);
        await _suspensionHandler.Received(2).OnYieldAsync(Arg.Any<DAsyncId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunsDTaskThatAwaitsWhenAll()
    {
        // Arrange
        var callback = Substitute.For<ISuspensionCallback>();

        static async DTask M1(ISuspensionCallback callback)
        {
            await DTask.WhenAll(
                M2(callback),
                DTask.FromResult(string.Empty),
                DTask.CompletedDTask,
                DTask.Delay(TimeSpan.FromMinutes(10))
            );
        }

        static async DTask M2(ISuspensionCallback callback)
        {
            await DTask.Factory.Callback(callback);
        }

        DAsyncId id1 = default;
        DAsyncId id2 = default;
        DAsyncId id3 = default;
        _suspensionHandler
            .When(host => host.OnYieldAsync(Arg.Any<DAsyncId>(), Arg.Any<CancellationToken>()))
            .Do(call => id1 = call.Arg<DAsyncId>());
        _suspensionHandler
            .When(host => host.OnDelayAsync(Arg.Any<DAsyncId>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()))
            .Do(call => id2 = call.Arg<DAsyncId>());
        callback
            .When(cb => cb.InvokeAsync(Arg.Any<DAsyncId>()))
            .Do(call => id3 = call.Arg<DAsyncId>());

        DTask task = M1(callback);

        // Act
        await _sut.StartAsync(_host, task);
        await _sut.ResumeAsync(_host, id1);
        await _sut.ResumeAsync(_host, id2);
        await _sut.ResumeAsync(_host, id3);

        // Assert
        await callback.Received(1).InvokeAsync(Arg.Any<DAsyncId>());
        await _suspensionHandler.Received(1).OnYieldAsync(Arg.Any<DAsyncId>(), Arg.Any<CancellationToken>());
        await _suspensionHandler.Received(1)
            .OnDelayAsync(Arg.Any<DAsyncId>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
        await _host.Received(1).OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunsDTaskThatAwaitsWhenAllOfResult()
    {
        // Arrange
        const int result1 = 1;
        const int result2 = 2;
        const int result3 = 3;

        static async DTask<int[]> M1()
        {
            DTask<int>[] tasks =
            [
                M2(result1),
                DTask.FromResult(result3),
                M2(result2)
            ];
            int[] results = await DTask.WhenAll(tasks);
            return results;
        }

        static async DTask<int> M2(int result)
        {
            await DTask.Yield();
            return result;
        }

        DAsyncId id1 = default;
        DAsyncId id2 = default;
        DAsyncId id3 = default;
        DAsyncId id4 = default;
        _suspensionHandler
            .When(handler => handler.OnYieldAsync(Arg.Any<DAsyncId>(), Arg.Any<CancellationToken>()))
            .Do(call =>
            {
                if (id1 == default)
                {
                    id1 = call.Arg<DAsyncId>();
                    return;
                }

                if (id2 == default)
                {
                    id2 = call.Arg<DAsyncId>();
                    return;
                }

                if (id3 == default)
                {
                    id3 = call.Arg<DAsyncId>();
                    return;
                }

                if (id4 == default)
                {
                    id4 = call.Arg<DAsyncId>();
                    return;
                }

                throw FailException.ForFailure("YieldAsync called too many times");
            });

        DTask task = M1();

        // Act
        await _sut.StartAsync(_host, task);
        await _sut.ResumeAsync(_host, id2);
        await _sut.ResumeAsync(_host, id1);
        await _sut.ResumeAsync(_host, id3);
        await _sut.ResumeAsync(_host, id4);

        // Assert
        task.Status.Should().Be(DTaskStatus.Suspended);
        await _suspensionHandler.Received(4).OnYieldAsync(Arg.Any<DAsyncId>(), Arg.Any<CancellationToken>());
        await _host.Received(1).OnSucceedAsync(
            Arg.Any<IDAsyncFlowCompletionContext>(),
            Arg.Is<int[]>(results => results.SequenceEqual(new[] { result1, result3, result2 })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunsDTaskThatAwaitsWhenAny()
    {
        // Arrange
        var callback = Substitute.For<ISuspensionCallback>();

        static async DTask<bool> M1(ISuspensionCallback callback)
        {
            DTask task1 = M2(callback);
            DTask task2 = DTask.Delay(TimeSpan.FromMinutes(10));

            DTask winner = await DTask.WhenAny(task1, task2);

            return winner == task1;
        }

        static async DTask M2(ISuspensionCallback callback)
        {
            await DTask.Factory.Callback(callback);
        }

        DAsyncId id1 = default;
        DAsyncId id2 = default;
        _suspensionHandler
            .When(handler => handler.OnYieldAsync(Arg.Any<DAsyncId>(), Arg.Any<CancellationToken>()))
            .Do(call => id1 = call.Arg<DAsyncId>());
        callback
            .When(cb => cb.InvokeAsync(Arg.Any<DAsyncId>()))
            .Do(call => id2 = call.Arg<DAsyncId>());

        DTask task = M1(callback);

        // Act
        await _sut.StartAsync(_host, task);
        await _sut.ResumeAsync(_host, id1);
        await _sut.ResumeAsync(_host, id2);

        // Assert
        await callback.Received(1).InvokeAsync(Arg.Any<DAsyncId>());
        await _suspensionHandler.Received(1).OnYieldAsync(Arg.Any<DAsyncId>(), Arg.Any<CancellationToken>());
        await _suspensionHandler.Received(1)
            .OnDelayAsync(Arg.Any<DAsyncId>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
        await _host.Received(1).OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), true, Arg.Any<CancellationToken>());

        // TODO: Restore this assertion when we properly manage to clean up the state machines of the handles
        // _stateManager.Count.Should().Be(0);
    }

    [Fact]
    public async Task AwaitingWhenAll_GivesAccessToIndividualResults()
    {
        // Arrange
        const int result1 = 1;
        const int result2 = 2;

        static async DTask<bool> M1()
        {
            DTask<int> task1 = M2(result1);
            DTask<int> task2 = M2(result2);
            await DTask.WhenAll(new DTask[] { task1, task2 });

            int result1Awaited = await task1;
            int result2Awaited = await task2;

            int result1Property = task1.Result;
            int result2Property = task2.Result;

            return
                result1Awaited == result1Property &&
                result2Awaited == result2Property;
        }

        static async DTask<int> M2(int result)
        {
            await DTask.Yield();
            return result;
        }

        DAsyncId id1 = default;
        DAsyncId id2 = default;
        DAsyncId id3 = default;
        DAsyncId id4 = default;
        _suspensionHandler
            .When(handler => handler.OnYieldAsync(Arg.Any<DAsyncId>(), Arg.Any<CancellationToken>()))
            .Do(call =>
            {
                if (id1 == default)
                {
                    id1 = call.Arg<DAsyncId>();
                    return;
                }

                if (id2 == default)
                {
                    id2 = call.Arg<DAsyncId>();
                    return;
                }

                if (id3 == default)
                {
                    id3 = call.Arg<DAsyncId>();
                    return;
                }

                if (id4 == default)
                {
                    id4 = call.Arg<DAsyncId>();
                    return;
                }

                throw FailException.ForFailure("YieldAsync called too many times");
            });

        DTask task = M1();

        // Act
        await _sut.StartAsync(_host, task);
        await _sut.ResumeAsync(_host, id2);
        await _sut.ResumeAsync(_host, id1);
        await _sut.ResumeAsync(_host, id3);
        await _sut.ResumeAsync(_host, id4);

        // Assert
        await _host.Received(1).OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), true, Arg.Any<CancellationToken>());

        // TODO: Restore this assertion when we properly manage to clean up the state machines of the handles
        // _stateManager.Count.Should().Be(0);
    }

    [Fact]
    public async Task AwaitingWhenAllOfResult_GivesAccessToIndividualResults()
    {
        // Arrange
        const int result1 = 1;
        const int result2 = 2;

        static async DTask<bool> M1()
        {
            DTask<int> task1 = M2(result1);
            DTask<int> task2 = M2(result2);
            int[] results = await DTask.WhenAll(task1, task2);

            int result1Awaited = await task1;
            int result2Awaited = await task2;

            int result1Property = task1.Result;
            int result2Property = task2.Result;

            return
                results[0] == result1Awaited &&
                results[0] == result1Property &&
                results[1] == result2Awaited &&
                results[1] == result2Property;
        }

        static async DTask<int> M2(int result)
        {
            await DTask.Yield();
            return result;
        }

        DAsyncId id1 = default;
        DAsyncId id2 = default;
        DAsyncId id3 = default;
        DAsyncId id4 = default;
        _suspensionHandler
            .When(handler => handler.OnYieldAsync(Arg.Any<DAsyncId>(), Arg.Any<CancellationToken>()))
            .Do(call =>
            {
                if (id1 == default)
                {
                    id1 = call.Arg<DAsyncId>();
                    return;
                }

                if (id2 == default)
                {
                    id2 = call.Arg<DAsyncId>();
                    return;
                }

                if (id3 == default)
                {
                    id3 = call.Arg<DAsyncId>();
                    return;
                }

                if (id4 == default)
                {
                    id4 = call.Arg<DAsyncId>();
                    return;
                }

                throw FailException.ForFailure("YieldAsync called too many times");
            });

        DTask task = M1();

        // Act
        await _sut.StartAsync(_host, task);
        await _sut.ResumeAsync(_host, id2);
        await _sut.ResumeAsync(_host, id1);
        await _sut.ResumeAsync(_host, id3);
        await _sut.ResumeAsync(_host, id4);

        // Assert
        await _host.Received(1).OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), true, Arg.Any<CancellationToken>());

        // TODO: Restore this assertion when we properly manage to clean up the state machines of the handles
        //_stateManager.Count.Should().Be(0);
    }

    [Fact(Skip = "Not implemented yet")]
    public async Task HandleStateMachinesAreClearedEvenIfNotAwaited()
    {
        // Arrange
        const int result1 = 1;
        const int result2 = 2;

        static async DTask M1()
        {
            DTask<int> task1 = M2(result1);
            DTask<int> task2 = M2(result2);
            int[] results = await DTask.WhenAll(task1, task2);

            await task1;
        }

        static async DTask<int> M2(int result)
        {
            await DTask.Yield();
            return result;
        }

        DAsyncId id1 = default;
        DAsyncId id2 = default;
        DAsyncId id3 = default;
        DAsyncId id4 = default;
        _suspensionHandler
            .When(handler => handler.OnYieldAsync(Arg.Any<DAsyncId>(), Arg.Any<CancellationToken>()))
            .Do(call =>
            {
                if (id1 == default)
                {
                    id1 = call.Arg<DAsyncId>();
                    return;
                }

                if (id2 == default)
                {
                    id2 = call.Arg<DAsyncId>();
                    return;
                }

                if (id3 == default)
                {
                    id3 = call.Arg<DAsyncId>();
                    return;
                }

                if (id4 == default)
                {
                    id4 = call.Arg<DAsyncId>();
                    return;
                }

                throw FailException.ForFailure("YieldAsync called too many times");
            });

        DTask task = M1();

        // Act
        await _sut.StartAsync(_host, task);
        await _sut.ResumeAsync(_host, id2);
        await _sut.ResumeAsync(_host, id1);
        await _sut.ResumeAsync(_host, id3);
        await _sut.ResumeAsync(_host, id4);

        // Assert
    }

    private static Expression<Predicate<DAsyncId>> NonReservedId => id => id != default && !id.IsFlowId;

    private sealed class YieldRunnable : IDAsyncRunnable
    {
        public void Run(IDAsyncRunner runner) => runner.Yield();
    }

    private sealed class ResumingDTask : DTask
    {
        public override DTaskStatus Status => DTaskStatus.Suspended;

        protected override void Run(IDAsyncRunner runner) => runner.Succeed();
    }

    private sealed class ResumingDTask<TResult>(TResult result) : DTask<TResult>
    {
        public override DTaskStatus Status => DTaskStatus.Suspended;

        protected override void Run(IDAsyncRunner runner) => runner.Succeed(result);
    }

    private sealed class ExceptionResumingDTask(Exception exception) : DTask
    {
        public override DTaskStatus Status => DTaskStatus.Suspended;

        protected override void Run(IDAsyncRunner runner) => runner.Fail(exception);
    }
}