#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using DTasks.Configuration;
using DTasks.Configuration.DependencyInjection;
using DTasks.Execution;
using DTasks.Infrastructure.DependencyInjection;
using DTasks.Infrastructure.Execution;
using DTasks.Infrastructure.Fakes;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;
using DTasks.Marshaling;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReceivedExtensions;

namespace DTasks.Infrastructure;

public sealed class DAsyncFlowTests
{
    private readonly FakeStorage _storage;
    private readonly FakeDAsyncIdFactory _idFactory;
    private readonly IDAsyncStack _stack;
    private readonly IDAsyncHeap _heap;
    private readonly IDAsyncSuspensionHandler _suspensionHandler;
    private readonly DAsyncFlow _sut;
    private readonly IDAsyncFlowPool _pool;
    private readonly IDAsyncInfrastructure _infrastructure;
    private readonly IDAsyncHost _host;
    private readonly CancellationToken _cancellationToken;

    public DAsyncFlowTests()
    {
        _storage = new();
        _idFactory = new();
        _stack = Substitute.For<IDAsyncStack>();
        _heap = Substitute.For<IDAsyncHeap>();
        _suspensionHandler = Substitute.For<IDAsyncSuspensionHandler>();

        var stateManagerDescriptor =
            from flow in ComponentDescriptors.Flow
            select new FakeDAsyncStateManager(_storage, _stack, _heap, flow.Parent.Parent.TypeResolver, flow.Surrogator);

        var suspensionHandlerDescriptor = ComponentDescriptor.Singleton(_suspensionHandler);
        
        DTasksConfigurationBuilder configurationBuilder = new();
        DAsyncFlow.ConfigureMarshaling(configurationBuilder);
        
        DAsyncInfrastructureBuilder infrastructureBuilder = new();
        infrastructureBuilder.UseStack(stateManagerDescriptor);
        infrastructureBuilder.UseHeap(stateManagerDescriptor);
        infrastructureBuilder.UseSuspensionHandler(suspensionHandlerDescriptor);
        
        _pool = Substitute.For<IDAsyncFlowPool>();
        _infrastructure = infrastructureBuilder.Build(configurationBuilder);
        _host = Substitute.For<IDAsyncHost>();
        _cancellationToken = new CancellationTokenSource().Token;

        _sut = new DAsyncFlow(_pool, _infrastructure, _idFactory);
#if DEBUG
        _sut.Initialize(new ContextRecordingDAsyncHost(_host), Environment.StackTrace);
#else
        _sut.Initialize(_host);
#endif
    }

    [Fact]
    public void NonInitializedFlow_ThrowsWhenUsed()
    {
        // Arrange
        var sut = new DAsyncFlow(_pool, _infrastructure, _idFactory);
        var runnable = Substitute.For<IDAsyncRunnable>();
        
        // Act
        Func<ValueTask> act = () => sut.StartAsync(runnable, _cancellationToken);
        
        // Assert
        act.Should().ThrowExactly<InvalidOperationException>();
    }

    [Fact]
    public void Dispose_ReturnsFlowToPool()
    {
        // Arrange
        
        // Act
        _sut.Dispose();
        
        // Assert
        _pool.Received().Return(_sut);
    }

    [Fact]
    public async Task CallingSetResultOnStartContext_MakesFlowReturn()
    {
        // Arrange
        var runnable = Substitute.For<IDAsyncRunnable>();

        _host
            .OnStartAsync(Arg.Any<IDAsyncFlowStartContext>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                call.Arg<IDAsyncFlowStartContext>().SetResult();
                return Task.CompletedTask;
            });
        
        // Act
        await _sut.StartAsync(runnable, _cancellationToken);

        // Assert
        runnable.DidNotReceive().Run(Arg.Any<IDAsyncRunner>());
        await _host.Received().OnStartAsync(Arg.Any<IDAsyncFlowStartContext>(), _cancellationToken);
    }

    [Fact]
    public async Task CallingSetExceptionOnStartContext_MakesFlowThrow()
    {
        // Arrange
        var runnable = Substitute.For<IDAsyncRunnable>();
        var expectedException = new Exception();

        _host
            .OnStartAsync(Arg.Any<IDAsyncFlowStartContext>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                call.Arg<IDAsyncFlowStartContext>().SetException(expectedException);
                return Task.CompletedTask;
            });
        
        // Act
        Exception? thrownException = await GetExceptionAsync(() => _sut.StartAsync(runnable, _cancellationToken));

        // Assert
        thrownException.Should().BeSameAs(expectedException);
        runnable.DidNotReceive().Run(Arg.Any<IDAsyncRunner>());
        await _host.Received().OnStartAsync(Arg.Any<IDAsyncFlowStartContext>(), _cancellationToken);
    }

    [Fact]
    public async Task ExceptionThrownSynchronouslyOnStart_MakesFlowThrowInfrastructureException()
    {
        // Arrange
        var runnable = Substitute.For<IDAsyncRunnable>();
        var expectedException = new Exception();

#pragma warning disable NS5003
        _host
            .OnStartAsync(Arg.Any<IDAsyncFlowStartContext>(), Arg.Any<CancellationToken>())
            .Throws(expectedException);
#pragma warning restore NS5003
        
        // Act
        Exception? thrownException = await GetExceptionAsync(() => _sut.StartAsync(runnable, _cancellationToken));

        // Assert
        thrownException.Should().BeOfType<DAsyncInfrastructureException>().Which.InnerException.Should().BeSameAs(expectedException);
        runnable.DidNotReceive().Run(Arg.Any<IDAsyncRunner>());
        await _host.Received().OnStartAsync(Arg.Any<IDAsyncFlowStartContext>(), _cancellationToken);
    }

    [Fact]
    public async Task ExceptionThrownAsynchronouslyOnStart_MakesFlowThrowInfrastructureException()
    {
        // Arrange
        var runnable = Substitute.For<IDAsyncRunnable>();
        var expectedException = new Exception();

        _host
            .OnStartAsync(Arg.Any<IDAsyncFlowStartContext>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(expectedException);
        
        // Act
        Exception? thrownException = await GetExceptionAsync(() => _sut.StartAsync(runnable, _cancellationToken));

        // Assert
        thrownException.Should().BeOfType<DAsyncInfrastructureException>().Which.InnerException.Should().BeSameAs(expectedException);
        runnable.DidNotReceive().Run(Arg.Any<IDAsyncRunner>());
        await _host.Received().OnStartAsync(Arg.Any<IDAsyncFlowStartContext>(), _cancellationToken);
    }

    [Fact]
    public async Task RunsSucceedRunnable()
    {
        // Arrange
        var runnable = new SucceedDAsyncRunnable();
    
        // Act
        await _sut.StartAsync(runnable, _cancellationToken);
    
        // Assert
        await _host.Received(1).OnSucceedAsync(Arg.Is<IDAsyncFlowCompletionContext>(ctx => ctx.FlowId.IsFlow), _cancellationToken);
    }

    [Fact]
    public async Task ExceptionThrownSynchronouslyOnSucceed_MakesFlowThrowInfrastructureException()
    {
        // Arrange
        var runnable = new SucceedDAsyncRunnable();
        var expectedException = new Exception();

#pragma warning disable NS5003
        _host
            .OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), Arg.Any<CancellationToken>())
            .Throws(expectedException);
#pragma warning restore NS5003
    
        // Act
        Exception? thrownException = await GetExceptionAsync(() => _sut.StartAsync(runnable, _cancellationToken));
    
        // Assert
        thrownException.Should().BeOfType<DAsyncInfrastructureException>().Which.InnerException.Should().BeSameAs(expectedException);
    }

    [Fact]
    public async Task ExceptionThrownAsynchronouslyOnSucceed_MakesFlowThrowInfrastructureException()
    {
        // Arrange
        var runnable = new SucceedDAsyncRunnable();
        var expectedException = new Exception();

        _host
            .OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(expectedException);
    
        // Act
        Exception? thrownException = await GetExceptionAsync(() => _sut.StartAsync(runnable, _cancellationToken));
    
        // Assert
        thrownException.Should().BeOfType<DAsyncInfrastructureException>().Which.InnerException.Should().BeSameAs(expectedException);
    }
    
    [Fact]
    public async Task RunsSucceedRunnableOfResult()
    {
        // Arrange
        const int result = 42;
        var runnable = new SucceedDAsyncRunnable<int>(result);
    
        // Act
        await _sut.StartAsync(runnable, _cancellationToken);
    
        // Assert
        await _host.Received(1).OnSucceedAsync(CompletionContext, result, _cancellationToken);
    }

    [Fact]
    public async Task ExceptionThrownSynchronouslyOnSucceedOfResult_MakesFlowThrowInfrastructureException()
    {
        // Arrange
        const int result = 42;
        var runnable = new SucceedDAsyncRunnable<int>(result);
        var expectedException = new Exception();

#pragma warning disable NS5003
        _host
            .OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), result, Arg.Any<CancellationToken>())
            .Throws(expectedException);
#pragma warning restore NS5003
    
        // Act
        Exception? thrownException = await GetExceptionAsync(() => _sut.StartAsync(runnable, _cancellationToken));
    
        // Assert
        thrownException.Should().BeOfType<DAsyncInfrastructureException>().Which.InnerException.Should().BeSameAs(expectedException);
    }

    [Fact]
    public async Task ExceptionThrownAsynchronouslyOnSucceedOfResult_MakesFlowThrowInfrastructureException()
    {
        // Arrange
        const int result = 42;
        var runnable = new SucceedDAsyncRunnable<int>(result);
        var expectedException = new Exception();

        _host
            .OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), result, Arg.Any<CancellationToken>())
            .ThrowsAsync(expectedException);
    
        // Act
        Exception? thrownException = await GetExceptionAsync(() => _sut.StartAsync(runnable, _cancellationToken));
    
        // Assert
        thrownException.Should().BeOfType<DAsyncInfrastructureException>().Which.InnerException.Should().BeSameAs(expectedException);
    }
    
    [Fact]
    public async Task RunsFailRunnable()
    {
        // Arrange
        Exception exception = new();
        var runnable = new FailDAsyncRunnable(exception);
    
        // Act
        await _sut.StartAsync(runnable, _cancellationToken);
    
        // Assert
        await _host.Received(1).OnFailAsync(CompletionContext, exception, _cancellationToken);
    }

    [Fact]
    public async Task ExceptionThrownSynchronouslyOnFail_MakesFlowThrowInfrastructureException()
    {
        // Arrange
        Exception exception = new();
        var runnable = new FailDAsyncRunnable(exception);
        var expectedException = new Exception();

#pragma warning disable NS5003
        _host
            .OnFailAsync(Arg.Any<IDAsyncFlowCompletionContext>(), exception, Arg.Any<CancellationToken>())
            .Throws(expectedException);
#pragma warning restore NS5003
    
        // Act
        Exception? thrownException = await GetExceptionAsync(() => _sut.StartAsync(runnable, _cancellationToken));
    
        // Assert
        thrownException.Should().BeOfType<DAsyncInfrastructureException>().Which.InnerException.Should().BeSameAs(expectedException);
    }

    [Fact]
    public async Task ExceptionThrownAsynchronouslyOnFail_MakesFlowThrowInfrastructureException()
    {
        // Arrange
        Exception exception = new();
        var runnable = new FailDAsyncRunnable(exception);
        var expectedException = new Exception();

        _host
            .OnFailAsync(Arg.Any<IDAsyncFlowCompletionContext>(), exception, Arg.Any<CancellationToken>())
            .ThrowsAsync(expectedException);
    
        // Act
        Exception? thrownException = await GetExceptionAsync(() => _sut.StartAsync(runnable, _cancellationToken));
    
        // Assert
        thrownException.Should().BeOfType<DAsyncInfrastructureException>().Which.InnerException.Should().BeSameAs(expectedException);
    }
    
    [Fact]
    public async Task RunsCancelRunnable()
    {
        // Arrange
        OperationCanceledException exception = new();
        var runnable = new CancelDAsyncRunnable(exception);
    
        // Act
        await _sut.StartAsync(runnable, _cancellationToken);
    
        // Assert
        await _host.Received(1).OnCancelAsync(CompletionContext, exception, _cancellationToken);
    }

    [Fact]
    public async Task ExceptionThrownSynchronouslyOnCancel_MakesFlowThrowInfrastructureException()
    {
        // Arrange
        OperationCanceledException exception = new();
        var runnable = new CancelDAsyncRunnable(exception);
        var expectedException = new Exception();

#pragma warning disable NS5003
        _host
            .OnCancelAsync(Arg.Any<IDAsyncFlowCompletionContext>(), exception, Arg.Any<CancellationToken>())
            .Throws(expectedException);
#pragma warning restore NS5003
    
        // Act
        Exception? thrownException = await GetExceptionAsync(() => _sut.StartAsync(runnable, _cancellationToken));
    
        // Assert
        thrownException.Should().BeOfType<DAsyncInfrastructureException>().Which.InnerException.Should().BeSameAs(expectedException);
    }

    [Fact]
    public async Task ExceptionThrownAsynchronouslyOnCancel_MakesFlowThrowInfrastructureException()
    {
        // Arrange
        OperationCanceledException exception = new();
        var runnable = new CancelDAsyncRunnable(exception);
        var expectedException = new Exception();

        _host
            .OnCancelAsync(Arg.Any<IDAsyncFlowCompletionContext>(), exception, Arg.Any<CancellationToken>())
            .ThrowsAsync(expectedException);
    
        // Act
        Exception? thrownException = await GetExceptionAsync(() => _sut.StartAsync(runnable, _cancellationToken));
    
        // Assert
        thrownException.Should().BeOfType<DAsyncInfrastructureException>().Which.InnerException.Should().BeSameAs(expectedException);
    }
    
    [Fact]
    public async Task RunsYieldRunnable()
    {
        // Arrange (1)
        var runnable = new YieldDAsyncRunnable();
        DAsyncId yieldId = _idFactory.GetTestId(1);
    
        // Act (1)
        await _sut.StartAsync(runnable, _cancellationToken);
    
        // Assert (1)
        await _stack.Received(1).DehydrateAsync(Suspending(yieldId), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _suspensionHandler.Received(1).OnYieldAsync(yieldId, _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        
        // Arrange (2)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (2)
        await _sut.ResumeAsync(yieldId, _cancellationToken);
        
        // Assert (2)
        await _stack.Received(1).HydrateAsync(Resuming(yieldId), _cancellationToken);
        await _host.Received(1).OnSucceedAsync(CompletionContext, _cancellationToken);
    }
    
    [Fact]
    public async Task RunsDelayRunnable()
    {
        // Arrange (1)
        TimeSpan delay = TimeSpan.FromDays(1);
        var runnable = new DelayDAsyncRunnable(delay);
        DAsyncId delayId = _idFactory.GetTestId(1);
    
        // Act (1)
        await _sut.StartAsync(runnable, _cancellationToken);
    
        // Assert (1)
        await _stack.Received(1).DehydrateAsync(Suspending(delayId), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _suspensionHandler.Received(1).OnDelayAsync(delayId, delay, _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        
        // Arrange (2)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (2)
        await _sut.ResumeAsync(delayId, _cancellationToken);
        
        // Assert (2)
        await _stack.Received(1).HydrateAsync(Resuming(delayId), _cancellationToken);
        await _host.Received(1).OnSucceedAsync(CompletionContext, _cancellationToken);
    }
    
    [Fact]
    public async Task RunsCallbackRunnable()
    {
        // Arrange (1)
        var callback = Substitute.For<ISuspensionCallback>();
        var runnable = new CallbackDAsyncRunnable(callback);
        DAsyncId callbackId = _idFactory.GetTestId(1);
    
        // Act (1)
        await _sut.StartAsync(runnable, _cancellationToken);
    
        // Assert (1)
        await _stack.Received(1).DehydrateAsync(Suspending(callbackId), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await callback.Received(1).InvokeAsync(callbackId, _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        
        // Arrange (2)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (2)
        await _sut.ResumeAsync(callbackId, _cancellationToken);
        
        // Assert (2)
        await _stack.Received(1).HydrateAsync(Resuming(callbackId), _cancellationToken);
        await _host.Received(1).OnSucceedAsync(CompletionContext, _cancellationToken);
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
        await _sut.StartAsync(task, _cancellationToken);
    
        // Assert
        await _host.Received(1).OnSucceedAsync(CompletionContext, result, _cancellationToken);
    }
    
    [Fact]
    public async Task RunsDTaskThatCompletesLocallyAndAsynchronously()
    {
        // Arrange
        const int result = 42;
    
        static async DTask<int> M1()
        {
            await Task.Delay(100, CancellationToken.None);
            return result;
        }
    
        DTask<int> task = M1();
    
        // Act
        await _sut.StartAsync(task, _cancellationToken);
    
        // Assert
        await _host.Received(1).OnSucceedAsync(CompletionContext, result, _cancellationToken);
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
        await _sut.StartAsync(task, _cancellationToken);
    
        // Assert
        await _host.Received(1).OnSucceedAsync(CompletionContext, result, _cancellationToken);
    }
    
    [Fact]
    public async Task RunsDTaskThatAwaitsSucceedSuspendedDTask()
    {
        // Arrange
        static async DTask M1()
        {
            await new SucceedSuspendedDTask();
        }
    
        DTask task = M1();
        DAsyncId m1Id = _idFactory.GetTestId(1);
        
        // Act
        await _sut.StartAsync(task, _cancellationToken);
    
        // Assert
        await _stack.Received(1).DehydrateAsync(Suspending(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Resuming(m1Id), _cancellationToken);
        await _host.Received(1).OnSucceedAsync(CompletionContext, _cancellationToken);
    }
    
    [Fact]
    public async Task RunsDTaskThatAwaitsSucceedSuspendedDTaskOfResult()
    {
        // Arrange
        const int result = 42;
        
        static async DTask<int> M1()
        {
            return await new SucceedSuspendedDTask<int>(result);
        }
    
        DTask task = M1();
        DAsyncId m1Id = _idFactory.GetTestId(1);
        
        // Act
        await _sut.StartAsync(task, _cancellationToken);
    
        // Assert
        await _stack.Received(1).DehydrateAsync(Suspending(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Resuming(m1Id), result, _cancellationToken);
        await _host.Received(1).OnSucceedAsync(CompletionContext, result, _cancellationToken);
    }
    
    [Fact]
    public async Task RunsDTaskThatAwaitsFailSuspendedDTask()
    {
        // Arrange
        Exception exception = new();
        
        async DTask M1()
        {
            await new FailSuspendedDTask(exception);
        }
    
        DTask task = M1();
        DAsyncId m1Id = _idFactory.GetTestId(1);
        
        // Act
        await _sut.StartAsync(task, _cancellationToken);
    
        // Assert
        await _stack.Received(1).DehydrateAsync(Suspending(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Resuming(m1Id), exception, _cancellationToken);
        await _host.Received(1).OnFailAsync(CompletionContext, exception, _cancellationToken);
    }
    
    [Fact]
    public async Task RunsDTaskThatAwaitsCancelSuspendedDTask()
    {
        // Arrange
        OperationCanceledException exception = new();
        
        async DTask M1()
        {
            await new CancelSuspendedDTask(exception);
        }
    
        DTask task = M1();
        DAsyncId m1Id = _idFactory.GetTestId(1);
        
        // Act
        await _sut.StartAsync(task, _cancellationToken);
    
        // Assert
        await _stack.Received(1).DehydrateAsync(Suspending(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Resuming(m1Id), exception as Exception, _cancellationToken);
        await _host.Received(1).OnCancelAsync(CompletionContext, exception, _cancellationToken);
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
        DAsyncId m1Id = _idFactory.GetTestId(1);
    
        // Act
        await _sut.StartAsync(task, _cancellationToken);
    
        // Assert
        await _stack.Received(1).DehydrateAsync(Suspending(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Resuming(m1Id), result, _cancellationToken);
        await _host.Received(1).OnSucceedAsync(CompletionContext, result + 1, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task RunsDTaskThatAwaitsYield()
    {
        // Arrange (1)
        const int result = 42;
    
        static async DTask<int> M1()
        {
            await DTask.Yield();
            return result;
        }
    
        DTask task = M1();
        DAsyncId m1Id = _idFactory.GetTestId(1);
        DAsyncId yieldId = _idFactory.GetTestId(2);
    
        // Act (1)
        await _sut.StartAsync(task, _cancellationToken);
        
        // Assert (1)
        await _stack.Received(1).DehydrateAsync(Suspending(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Suspending(yieldId, m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _suspensionHandler.Received(1).OnYieldAsync(yieldId, _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        
        // Arrange (2)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (2)
        await _sut.ResumeAsync(yieldId, _cancellationToken);
    
        // Assert (2)
        await _stack.Received(1).HydrateAsync(Resuming(yieldId), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Resuming(m1Id), _cancellationToken);
        await _host.Received(1).OnSucceedAsync(CompletionContext, result, _cancellationToken);
    }
    
    [Fact]
    public async Task RunsDTaskThatAwaitsDelay()
    {
        // Arrange (1)
        const int result = 42;
        TimeSpan delay = TimeSpan.FromDays(1);
    
        static async DTask<int> M1(TimeSpan delay)
        {
            await DTask.Delay(delay);
            return result;
        }
    
        DTask task = M1(delay);
        DAsyncId m1Id = _idFactory.GetTestId(1);
        DAsyncId delayId = _idFactory.GetTestId(2);
    
        // Act (1)
        await _sut.StartAsync(task, _cancellationToken);
        
        // Assert (1)
        await _stack.Received(1).DehydrateAsync(Suspending(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Suspending(delayId, m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _suspensionHandler.Received(1).OnDelayAsync(delayId, delay, _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        
        // Arrange (2)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (2)
        await _sut.ResumeAsync(delayId, _cancellationToken);
    
        // Assert (2)
        await _stack.Received(1).HydrateAsync(Resuming(delayId), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Resuming(m1Id), _cancellationToken);
        await _host.Received(1).OnSucceedAsync(CompletionContext, result, _cancellationToken);
    }
    
    [Fact]
    public async Task RunsDTaskThatAwaitsCallback()
    {
        // Arrange (1)
        const int result = 42;
        var callback = Substitute.For<ISuspensionCallback>();
    
        static async DTask<int> M1(ISuspensionCallback callback)
        {
            await DTask.Factory.Suspend(callback);
            return result;
        }
    
        DTask task = M1(callback);
        DAsyncId m1Id = _idFactory.GetTestId(1);
        DAsyncId callbackId = _idFactory.GetTestId(2);
    
        // Act (1)
        await _sut.StartAsync(task, _cancellationToken);
        
        // Assert (1)
        await _stack.Received(1).DehydrateAsync(Suspending(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Suspending(callbackId, m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await callback.Received(1).InvokeAsync(callbackId, _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        
        // Arrange (2)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (2)
        await _sut.ResumeAsync(callbackId, _cancellationToken);
    
        // Assert (2)
        await _stack.Received(1).HydrateAsync(Resuming(callbackId), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Resuming(m1Id), _cancellationToken);
        await _host.Received(1).OnSucceedAsync(CompletionContext, result, _cancellationToken);
    }
    
    [Fact]
    public async Task RunsDTaskThatAwaitsCallbackOfResult()
    {
        // Arrange (1)
        const int result = 42;
        var callback = Substitute.For<ISuspensionCallback>();
    
        static async DTask<int> M1(ISuspensionCallback callback)
        {
            return await DTask<int>.Factory.Suspend(callback);
        }
    
        DTask task = M1(callback);
        DAsyncId m1Id = _idFactory.GetTestId(1);
        DAsyncId callbackId = _idFactory.GetTestId(2);
    
        // Act (1)
        await _sut.StartAsync(task, _cancellationToken);
        
        // Assert (1)
        await _stack.Received(1).DehydrateAsync(Suspending(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Suspending(callbackId, m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await callback.Received(1).InvokeAsync(callbackId, _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        
        // Arrange (2)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (2)
        await _sut.ResumeAsync(callbackId, result, _cancellationToken);
    
        // Assert (2)
        await _stack.Received(1).HydrateAsync(Resuming(callbackId), result, _cancellationToken);
        await _stack.Received(1).HydrateAsync(Resuming(m1Id), result, _cancellationToken);
        await _host.Received(1).OnSucceedAsync(CompletionContext, result, _cancellationToken);
    }
    
    [Fact]
    public async Task YieldingTwiceSavesStateWithSameIdWithoutExposingIt()
    {
        // Arrange (1)
        static async DTask M1()
        {
            await DTask.Yield();
            await DTask.Yield();
        }
    
        DTask task = M1();
        DAsyncId m1Id = _idFactory.GetTestId(1);
        DAsyncId yieldId1 = _idFactory.GetTestId(2);
        DAsyncId yieldId2 = _idFactory.GetTestId(3);
    
        // Act (1)
        await _sut.StartAsync(task, _cancellationToken);
        
        // Assert (1)
        await _stack.Received(1).DehydrateAsync(Suspending(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Suspending(yieldId1, m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _suspensionHandler.Received(1).OnYieldAsync(yieldId1, _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        
        // Arrange (2)
        _stack.ClearReceivedCalls();
        _suspensionHandler.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (2)
        await _sut.ResumeAsync(yieldId1, _cancellationToken);
        
        // Assert (2)
        await _stack.Received(1).HydrateAsync(Resuming(yieldId1), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Resuming(m1Id), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Suspending(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Suspending(yieldId2, m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _suspensionHandler.Received(1).OnYieldAsync(yieldId2, _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        
        // Arrange (3)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (3)
        await _sut.ResumeAsync(yieldId2, _cancellationToken);
    
        // Assert (3)
        await _stack.Received(1).HydrateAsync(Resuming(yieldId2), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Resuming(m1Id), _cancellationToken);
        await _host.Received(1).OnSucceedAsync(CompletionContext, _cancellationToken);
    }

    [Fact]
    public async Task MarshalingDTask_Throws_WhenNotAwaitedOrMarshaled()
    {
        // Arrange
        static async DTask M1()
        {
            DTask task = DTask.CompletedDTask;
            await DTask.Yield();
            _ = task.Status; // Keeps 'task' among the state machine fields
        }

        DTask task = M1();
        
        // Act
        Func<Task> act = async () => await _sut.StartAsync(task, _cancellationToken);
        
        // Assert
        await act.Should().ThrowAsync<MarshalingException>();
    }

    [Fact]
    public async Task MarshalingDTask_SurrogatesTask_WhenExplicitlyMarshaled()
    {
        // Arrange (1)
        static async DTask<DTaskStatus> M1()
        {
            DTask task = M2();
            await task.MarshalDAsync();
            await DTask.Yield();
            return task.Status;
        }

        static async DTask M2()
        {
        }

        DTask task = M1();
        DAsyncId m1Id = _idFactory.GetTestId(1);
        DAsyncId m2Id = _idFactory.GetTestId(2);
        DAsyncId yieldId = _idFactory.GetTestId(3);
        
        // Act (1)
        await _sut.StartAsync(task, _cancellationToken);
        
        // Assert (1)
        await _stack.Received(1).DehydrateAsync(Suspending(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Suspending(m2Id, default), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Resuming(m1Id), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Suspending(yieldId, m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _suspensionHandler.Received(1).OnYieldAsync(yieldId, _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        
        // Arrange (2)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (2)
        await _sut.ResumeAsync(yieldId, _cancellationToken);
        
        // Assert (2)
        await _stack.Received(1).HydrateAsync(Resuming(yieldId), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Resuming(m1Id), _cancellationToken);
        await _host.Received(1).OnSucceedAsync(CompletionContext, DTaskStatus.Pending, _cancellationToken);
    }
    
    //
    // [Fact]
    // public async Task RunsDTaskThatAwaitsWhenAll()
    // {
    //     // Arrange
    //     var callback = Substitute.For<ISuspensionCallback>();
    //
    //     static async DTask M1(ISuspensionCallback callback)
    //     {
    //         await DTask.WhenAll(
    //             M2(callback),
    //             DTask.FromResult(string.Empty),
    //             DTask.CompletedDTask,
    //             DTask.Delay(TimeSpan.FromMinutes(10))
    //         );
    //     }
    //
    //     static async DTask M2(ISuspensionCallback callback)
    //     {
    //         await DTask.Factory.Suspend(callback);
    //     }
    //
    //     DAsyncId id1 = default;
    //     DAsyncId id2 = default;
    //     DAsyncId id3 = default;
    //     _suspensionHandler
    //         .When(host => host.OnYieldAsync(Arg.Any<DAsyncId>(), Arg.Any<CancellationToken>()))
    //         .Do(call => id1 = call.Arg<DAsyncId>());
    //     _suspensionHandler
    //         .When(host => host.OnDelayAsync(Arg.Any<DAsyncId>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()))
    //         .Do(call => id2 = call.Arg<DAsyncId>());
    //     callback
    //         .When(cb => cb.InvokeAsync(Arg.Any<DAsyncId>()))
    //         .Do(call => id3 = call.Arg<DAsyncId>());
    //
    //     DTask task = M1(callback);
    //
    //     // Act
    //     await _sut.StartAsync(task);
    //     await _sut.ResumeAsync(id1);
    //     await _sut.ResumeAsync(id2);
    //     await _sut.ResumeAsync(id3);
    //
    //     // Assert
    //     await callback.Received(1).InvokeAsync(Arg.Any<DAsyncId>());
    //     await _suspensionHandler.Received(1).OnYieldAsync(Arg.Any<DAsyncId>(), Arg.Any<CancellationToken>());
    //     await _suspensionHandler.Received(1)
    //         .OnDelayAsync(Arg.Any<DAsyncId>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
    //     await _host.Received(1).OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), Arg.Any<CancellationToken>());
    // }
    //
    // [Fact]
    // public async Task RunsDTaskThatAwaitsWhenAllOfResult()
    // {
    //     // Arrange
    //     const int result1 = 1;
    //     const int result2 = 2;
    //     const int result3 = 3;
    //
    //     static async DTask<int[]> M1()
    //     {
    //         DTask<int>[] tasks =
    //         [
    //             M2(result1),
    //             DTask.FromResult(result3),
    //             M2(result2)
    //         ];
    //         int[] results = await DTask.WhenAll(tasks);
    //         return results;
    //     }
    //
    //     static async DTask<int> M2(int result)
    //     {
    //         await DTask.Yield();
    //         return result;
    //     }
    //
    //     DAsyncId id1 = default;
    //     DAsyncId id2 = default;
    //     DAsyncId id3 = default;
    //     DAsyncId id4 = default;
    //     _suspensionHandler
    //         .When(handler => handler.OnYieldAsync(Arg.Any<DAsyncId>(), Arg.Any<CancellationToken>()))
    //         .Do(call =>
    //         {
    //             if (id1 == default)
    //             {
    //                 id1 = call.Arg<DAsyncId>();
    //                 return;
    //             }
    //
    //             if (id2 == default)
    //             {
    //                 id2 = call.Arg<DAsyncId>();
    //                 return;
    //             }
    //
    //             if (id3 == default)
    //             {
    //                 id3 = call.Arg<DAsyncId>();
    //                 return;
    //             }
    //
    //             if (id4 == default)
    //             {
    //                 id4 = call.Arg<DAsyncId>();
    //                 return;
    //             }
    //
    //             throw FailException.ForFailure("YieldAsync called too many times");
    //         });
    //
    //     DTask task = M1();
    //
    //     // Act
    //     await _sut.StartAsync(task);
    //     await _sut.ResumeAsync(id2);
    //     await _sut.ResumeAsync(id1);
    //     await _sut.ResumeAsync(id3);
    //     await _sut.ResumeAsync(id4);
    //
    //     // Assert
    //     task.Status.Should().Be(DTaskStatus.Suspended);
    //     await _suspensionHandler.Received(4).OnYieldAsync(Arg.Any<DAsyncId>(), Arg.Any<CancellationToken>());
    //     await _host.Received(1).OnSucceedAsync(
    //         Arg.Any<IDAsyncFlowCompletionContext>(),
    //         Arg.Is<int[]>(results => results.SequenceEqual(new[] { result1, result3, result2 })),
    //         Arg.Any<CancellationToken>());
    // }
    //
    // [Fact]
    // public async Task RunsDTaskThatAwaitsWhenAny()
    // {
    //     // Arrange
    //     var callback = Substitute.For<ISuspensionCallback>();
    //
    //     static async DTask<bool> M1(ISuspensionCallback callback)
    //     {
    //         DTask task1 = M2(callback);
    //         DTask task2 = DTask.Delay(TimeSpan.FromMinutes(10));
    //
    //         DTask winner = await DTask.WhenAny(task1, task2);
    //
    //         return winner == task1;
    //     }
    //
    //     static async DTask M2(ISuspensionCallback callback)
    //     {
    //         await DTask.Factory.Suspend(callback);
    //     }
    //
    //     DAsyncId id1 = default;
    //     DAsyncId id2 = default;
    //     _suspensionHandler
    //         .When(handler => handler.OnYieldAsync(Arg.Any<DAsyncId>(), Arg.Any<CancellationToken>()))
    //         .Do(call => id1 = call.Arg<DAsyncId>());
    //     callback
    //         .When(cb => cb.InvokeAsync(Arg.Any<DAsyncId>()))
    //         .Do(call => id2 = call.Arg<DAsyncId>());
    //
    //     DTask task = M1(callback);
    //
    //     // Act
    //     await _sut.StartAsync(task);
    //     await _sut.ResumeAsync(id1);
    //     await _sut.ResumeAsync(id2);
    //
    //     // Assert
    //     await callback.Received(1).InvokeAsync(Arg.Any<DAsyncId>());
    //     await _suspensionHandler.Received(1).OnYieldAsync(Arg.Any<DAsyncId>(), Arg.Any<CancellationToken>());
    //     await _suspensionHandler.Received(1)
    //         .OnDelayAsync(Arg.Any<DAsyncId>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
    //     await _host.Received(1).OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), true, Arg.Any<CancellationToken>());
    //
    //     // TODO: Restore this assertion when we properly manage to clean up the state machines of the handles
    //     // _stateManager.Count.Should().Be(0);
    // }
    //
    // [Fact]
    // public async Task AwaitingWhenAll_GivesAccessToIndividualResults()
    // {
    //     // Arrange
    //     const int result1 = 1;
    //     const int result2 = 2;
    //
    //     static async DTask<bool> M1()
    //     {
    //         DTask<int> task1 = M2(result1);
    //         DTask<int> task2 = M2(result2);
    //         await DTask.WhenAll(new DTask[] { task1, task2 });
    //
    //         int result1Awaited = await task1;
    //         int result2Awaited = await task2;
    //
    //         int result1Property = task1.Result;
    //         int result2Property = task2.Result;
    //
    //         return
    //             result1Awaited == result1Property &&
    //             result2Awaited == result2Property;
    //     }
    //
    //     static async DTask<int> M2(int result)
    //     {
    //         await DTask.Yield();
    //         return result;
    //     }
    //
    //     DAsyncId id1 = default;
    //     DAsyncId id2 = default;
    //     DAsyncId id3 = default;
    //     DAsyncId id4 = default;
    //     _suspensionHandler
    //         .When(handler => handler.OnYieldAsync(Arg.Any<DAsyncId>(), Arg.Any<CancellationToken>()))
    //         .Do(call =>
    //         {
    //             if (id1 == default)
    //             {
    //                 id1 = call.Arg<DAsyncId>();
    //                 return;
    //             }
    //
    //             if (id2 == default)
    //             {
    //                 id2 = call.Arg<DAsyncId>();
    //                 return;
    //             }
    //
    //             if (id3 == default)
    //             {
    //                 id3 = call.Arg<DAsyncId>();
    //                 return;
    //             }
    //
    //             if (id4 == default)
    //             {
    //                 id4 = call.Arg<DAsyncId>();
    //                 return;
    //             }
    //
    //             throw FailException.ForFailure("YieldAsync called too many times");
    //         });
    //
    //     DTask task = M1();
    //
    //     // Act
    //     await _sut.StartAsync(task);
    //     await _sut.ResumeAsync(id2);
    //     await _sut.ResumeAsync(id1);
    //     await _sut.ResumeAsync(id3);
    //     await _sut.ResumeAsync(id4);
    //
    //     // Assert
    //     await _host.Received(1).OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), true, Arg.Any<CancellationToken>());
    //
    //     // TODO: Restore this assertion when we properly manage to clean up the state machines of the handles
    //     // _stateManager.Count.Should().Be(0);
    // }
    //
    // [Fact]
    // public async Task AwaitingWhenAllOfResult_GivesAccessToIndividualResults()
    // {
    //     // Arrange
    //     const int result1 = 1;
    //     const int result2 = 2;
    //
    //     static async DTask<bool> M1()
    //     {
    //         DTask<int> task1 = M2(result1);
    //         DTask<int> task2 = M2(result2);
    //         int[] results = await DTask.WhenAll(task1, task2);
    //
    //         int result1Awaited = await task1;
    //         int result2Awaited = await task2;
    //
    //         int result1Property = task1.Result;
    //         int result2Property = task2.Result;
    //
    //         return
    //             results[0] == result1Awaited &&
    //             results[0] == result1Property &&
    //             results[1] == result2Awaited &&
    //             results[1] == result2Property;
    //     }
    //
    //     static async DTask<int> M2(int result)
    //     {
    //         await DTask.Yield();
    //         return result;
    //     }
    //
    //     DAsyncId id1 = default;
    //     DAsyncId id2 = default;
    //     DAsyncId id3 = default;
    //     DAsyncId id4 = default;
    //     _suspensionHandler
    //         .When(handler => handler.OnYieldAsync(Arg.Any<DAsyncId>(), Arg.Any<CancellationToken>()))
    //         .Do(call =>
    //         {
    //             if (id1 == default)
    //             {
    //                 id1 = call.Arg<DAsyncId>();
    //                 return;
    //             }
    //
    //             if (id2 == default)
    //             {
    //                 id2 = call.Arg<DAsyncId>();
    //                 return;
    //             }
    //
    //             if (id3 == default)
    //             {
    //                 id3 = call.Arg<DAsyncId>();
    //                 return;
    //             }
    //
    //             if (id4 == default)
    //             {
    //                 id4 = call.Arg<DAsyncId>();
    //                 return;
    //             }
    //
    //             throw FailException.ForFailure("YieldAsync called too many times");
    //         });
    //
    //     DTask task = M1();
    //
    //     // Act
    //     await _sut.StartAsync(task);
    //     await _sut.ResumeAsync(id2);
    //     await _sut.ResumeAsync(id1);
    //     await _sut.ResumeAsync(id3);
    //     await _sut.ResumeAsync(id4);
    //
    //     // Assert
    //     await _host.Received(1).OnSucceedAsync(Arg.Any<IDAsyncFlowCompletionContext>(), true, Arg.Any<CancellationToken>());
    //
    //     // TODO: Restore this assertion when we properly manage to clean up the state machines of the handles
    //     //_stateManager.Count.Should().Be(0);
    // }
    //
    // [Fact(Skip = "Not implemented yet")]
    // public async Task HandleStateMachinesAreClearedEvenIfNotAwaited()
    // {
    //     // Arrange
    //     const int result1 = 1;
    //     const int result2 = 2;
    //
    //     static async DTask M1()
    //     {
    //         DTask<int> task1 = M2(result1);
    //         DTask<int> task2 = M2(result2);
    //         int[] results = await DTask.WhenAll(task1, task2);
    //
    //         await task1;
    //     }
    //
    //     static async DTask<int> M2(int result)
    //     {
    //         await DTask.Yield();
    //         return result;
    //     }
    //
    //     DAsyncId id1 = default;
    //     DAsyncId id2 = default;
    //     DAsyncId id3 = default;
    //     DAsyncId id4 = default;
    //     _suspensionHandler
    //         .When(handler => handler.OnYieldAsync(Arg.Any<DAsyncId>(), Arg.Any<CancellationToken>()))
    //         .Do(call =>
    //         {
    //             if (id1 == default)
    //             {
    //                 id1 = call.Arg<DAsyncId>();
    //                 return;
    //             }
    //
    //             if (id2 == default)
    //             {
    //                 id2 = call.Arg<DAsyncId>();
    //                 return;
    //             }
    //
    //             if (id3 == default)
    //             {
    //                 id3 = call.Arg<DAsyncId>();
    //                 return;
    //             }
    //
    //             if (id4 == default)
    //             {
    //                 id4 = call.Arg<DAsyncId>();
    //                 return;
    //             }
    //
    //             throw FailException.ForFailure("YieldAsync called too many times");
    //         });
    //
    //     DTask task = M1();
    //
    //     // Act
    //     await _sut.StartAsync(task);
    //     await _sut.ResumeAsync(id2);
    //     await _sut.ResumeAsync(id1);
    //     await _sut.ResumeAsync(id3);
    //     await _sut.ResumeAsync(id4);
    //
    //     // Assert
    // }
    
    private static ref IDAsyncFlowCompletionContext CompletionContext => ref Arg.Is<IDAsyncFlowCompletionContext>(ctx => ctx.FlowId == FakeDAsyncIdFactory.TestRootId);

    private static ref ISuspensionContext Suspending(DAsyncId id) => ref Arg.Is<ISuspensionContext>(ctx => ctx.Id == id && ctx.ParentId == FakeDAsyncIdFactory.TestRootId);

    private static ref ISuspensionContext Suspending(DAsyncId id, DAsyncId parentId) => ref Arg.Is<ISuspensionContext>(ctx => ctx.Id == id && ctx.ParentId == parentId);

    private static ref IResumptionContext Resuming(DAsyncId id) => ref Arg.Is<IResumptionContext>(ctx => ctx.Id == id);
    
    private static async Task<Exception?> GetExceptionAsync(Func<ValueTask> act)
    {
        try
        {
            await act();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}