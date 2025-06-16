#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using DTasks.Configuration;
using DTasks.Configuration.DependencyInjection;
using DTasks.Execution;
using DTasks.Infrastructure.DependencyInjection;
using DTasks.Infrastructure.Execution;
using DTasks.Infrastructure.Fakes;
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
        configurationBuilder.ConfigureInfrastructure(infrastructure => infrastructure
            .SurrogateDTaskOf<int>());
        
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
        await _stack.Received(1).DehydrateAsync(Dehydrating(yieldId), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _suspensionHandler.Received(1).OnYieldAsync(yieldId, _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        
        // Arrange (2)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (2)
        await _sut.ResumeAsync(yieldId, _cancellationToken);
        
        // Assert (2)
        await _stack.Received(1).HydrateAsync(Hydrating(yieldId), _cancellationToken);
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
        await _stack.Received(1).DehydrateAsync(Dehydrating(delayId), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _suspensionHandler.Received(1).OnDelayAsync(delayId, delay, _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        
        // Arrange (2)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (2)
        await _sut.ResumeAsync(delayId, _cancellationToken);
        
        // Assert (2)
        await _stack.Received(1).HydrateAsync(Hydrating(delayId), _cancellationToken);
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
        await _stack.Received(1).DehydrateAsync(Dehydrating(callbackId), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await callback.Received(1).InvokeAsync(callbackId, _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        
        // Arrange (2)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (2)
        await _sut.ResumeAsync(callbackId, _cancellationToken);
        
        // Assert (2)
        await _stack.Received(1).HydrateAsync(Hydrating(callbackId), _cancellationToken);
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
    
        var runnable = M1();
    
        // Act
        await _sut.StartAsync(runnable, _cancellationToken);
    
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
    
        var runnable = M1();
    
        // Act
        await _sut.StartAsync(runnable, _cancellationToken);
    
        // Assert
        await _host.Received(1).OnSucceedAsync(CompletionContext, result, _cancellationToken);
    }
    
    [Fact]
    public async Task AwaitsCompletedDTask()
    {
        // Arrange
        const int result = 42;
    
        static async DTask<int> M1()
        {
            await DTask.CompletedDTask;
            return result;
        }
    
        var runnable = M1();
    
        // Act
        await _sut.StartAsync(runnable, _cancellationToken);
    
        // Assert
        await _host.Received(1).OnSucceedAsync(CompletionContext, result, _cancellationToken);
    }
    
    [Fact]
    public async Task AwaitsSucceedSuspendedDTask()
    {
        // Arrange
        static async DTask M1()
        {
            await new SucceedSuspendedDTask();
        }
    
        var runnable = M1();
        DAsyncId m1Id = _idFactory.GetTestId(1);
        
        // Act
        await _sut.StartAsync(runnable, _cancellationToken);
    
        // Assert
        await _stack.Received(1).DehydrateAsync(Dehydrating(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(m1Id), _cancellationToken);
        await _host.Received(1).OnSucceedAsync(CompletionContext, _cancellationToken);
    }
    
    [Fact]
    public async Task AwaitsSucceedSuspendedDTaskOfResult()
    {
        // Arrange
        const int result = 42;
        
        static async DTask<int> M1()
        {
            return await new SucceedSuspendedDTask<int>(result);
        }
    
        var runnable = M1();
        DAsyncId m1Id = _idFactory.GetTestId(1);
        
        // Act
        await _sut.StartAsync(runnable, _cancellationToken);
    
        // Assert
        await _stack.Received(1).DehydrateAsync(Dehydrating(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(m1Id), result, _cancellationToken);
        await _host.Received(1).OnSucceedAsync(CompletionContext, result, _cancellationToken);
    }
    
    [Fact]
    public async Task AwaitsFailSuspendedDTask()
    {
        // Arrange
        Exception exception = new();
        
        async DTask M1()
        {
            await new FailSuspendedDTask(exception);
        }
    
        var runnable = M1();
        DAsyncId m1Id = _idFactory.GetTestId(1);
        
        // Act
        await _sut.StartAsync(runnable, _cancellationToken);
    
        // Assert
        await _stack.Received(1).DehydrateAsync(Dehydrating(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(m1Id), exception, _cancellationToken);
        await _host.Received(1).OnFailAsync(CompletionContext, exception, _cancellationToken);
    }
    
    [Fact]
    public async Task AwaitsCancelSuspendedDTask()
    {
        // Arrange
        OperationCanceledException exception = new();
        
        async DTask M1()
        {
            await new CancelSuspendedDTask(exception);
        }
    
        var runnable = M1();
        DAsyncId m1Id = _idFactory.GetTestId(1);
        
        // Act
        await _sut.StartAsync(runnable, _cancellationToken);
    
        // Assert
        await _stack.Received(1).DehydrateAsync(Dehydrating(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(m1Id), exception as Exception, _cancellationToken);
        await _host.Received(1).OnCancelAsync(CompletionContext, exception, _cancellationToken);
    }
    
    [Fact]
    public async Task AwaitsLocallyCompletingDTask()
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
    
        var runnable = M1();
        DAsyncId m1Id = _idFactory.GetTestId(1);
    
        // Act
        await _sut.StartAsync(runnable, _cancellationToken);
    
        // Assert
        await _stack.Received(1).DehydrateAsync(Dehydrating(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(m1Id), result, _cancellationToken);
        await _host.Received(1).OnSucceedAsync(CompletionContext, result + 1, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task AwaitsYield()
    {
        // Arrange (1)
        const int result = 42;
    
        static async DTask<int> M1()
        {
            await DTask.Yield();
            return result;
        }
    
        var runnable = M1();
        DAsyncId m1Id = _idFactory.GetTestId(1);
        DAsyncId yieldId = _idFactory.GetTestId(2);
    
        // Act (1)
        await _sut.StartAsync(runnable, _cancellationToken);
        
        // Assert (1)
        await _stack.Received(1).DehydrateAsync(Dehydrating(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(yieldId, m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _suspensionHandler.Received(1).OnYieldAsync(yieldId, _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        
        // Arrange (2)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (2)
        await _sut.ResumeAsync(yieldId, _cancellationToken);
    
        // Assert (2)
        await _stack.Received(1).HydrateAsync(Hydrating(yieldId), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(m1Id), _cancellationToken);
        await _host.Received(1).OnSucceedAsync(CompletionContext, result, _cancellationToken);
    }
    
    [Fact]
    public async Task AwaitsDelay()
    {
        // Arrange (1)
        const int result = 42;
        TimeSpan delay = TimeSpan.FromDays(1);
    
        static async DTask<int> M1(TimeSpan delay)
        {
            await DTask.Delay(delay);
            return result;
        }
    
        var runnable = M1(delay);
        DAsyncId m1Id = _idFactory.GetTestId(1);
        DAsyncId delayId = _idFactory.GetTestId(2);
    
        // Act (1)
        await _sut.StartAsync(runnable, _cancellationToken);
        
        // Assert (1)
        await _stack.Received(1).DehydrateAsync(Dehydrating(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(delayId, m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _suspensionHandler.Received(1).OnDelayAsync(delayId, delay, _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        
        // Arrange (2)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (2)
        await _sut.ResumeAsync(delayId, _cancellationToken);
    
        // Assert (2)
        await _stack.Received(1).HydrateAsync(Hydrating(delayId), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(m1Id), _cancellationToken);
        await _host.Received(1).OnSucceedAsync(CompletionContext, result, _cancellationToken);
    }
    
    [Fact]
    public async Task AwaitsCallback()
    {
        // Arrange (1)
        const int result = 42;
        var callback = Substitute.For<ISuspensionCallback>();
    
        static async DTask<int> M1(ISuspensionCallback callback)
        {
            await DTask.Factory.Suspend(callback);
            return result;
        }
    
        var runnable = M1(callback);
        DAsyncId m1Id = _idFactory.GetTestId(1);
        DAsyncId callbackId = _idFactory.GetTestId(2);
    
        // Act (1)
        await _sut.StartAsync(runnable, _cancellationToken);
        
        // Assert (1)
        await _stack.Received(1).DehydrateAsync(Dehydrating(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(callbackId, m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await callback.Received(1).InvokeAsync(callbackId, _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        
        // Arrange (2)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (2)
        await _sut.ResumeAsync(callbackId, _cancellationToken);
    
        // Assert (2)
        await _stack.Received(1).HydrateAsync(Hydrating(callbackId), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(m1Id), _cancellationToken);
        await _host.Received(1).OnSucceedAsync(CompletionContext, result, _cancellationToken);
    }
    
    [Fact]
    public async Task AwaitsCallbackOfResult()
    {
        // Arrange (1)
        const int result = 42;
        var callback = Substitute.For<ISuspensionCallback>();
    
        static async DTask<int> M1(ISuspensionCallback callback)
        {
            return await DTask<int>.Factory.Suspend(callback);
        }
    
        var runnable = M1(callback);
        DAsyncId m1Id = _idFactory.GetTestId(1);
        DAsyncId callbackId = _idFactory.GetTestId(2);
    
        // Act (1)
        await _sut.StartAsync(runnable, _cancellationToken);
        
        // Assert (1)
        await _stack.Received(1).DehydrateAsync(Dehydrating(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(callbackId, m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await callback.Received(1).InvokeAsync(callbackId, _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        
        // Arrange (2)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (2)
        await _sut.ResumeAsync(callbackId, result, _cancellationToken);
    
        // Assert (2)
        await _stack.Received(1).HydrateAsync(Hydrating(callbackId), result, _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(m1Id), result, _cancellationToken);
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
    
        var runnable = M1();
        DAsyncId m1Id = _idFactory.GetTestId(1);
        DAsyncId yieldId1 = _idFactory.GetTestId(2);
        DAsyncId yieldId2 = _idFactory.GetTestId(3);
    
        // Act (1)
        await _sut.StartAsync(runnable, _cancellationToken);
        
        // Assert (1)
        await _stack.Received(1).DehydrateAsync(Dehydrating(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(yieldId1, m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _suspensionHandler.Received(1).OnYieldAsync(yieldId1, _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        
        // Arrange (2)
        _stack.ClearReceivedCalls();
        _suspensionHandler.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (2)
        await _sut.ResumeAsync(yieldId1, _cancellationToken);
        
        // Assert (2)
        await _stack.Received(1).HydrateAsync(Hydrating(yieldId1), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(m1Id), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(yieldId2, m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _suspensionHandler.Received(1).OnYieldAsync(yieldId2, _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        
        // Arrange (3)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (3)
        await _sut.ResumeAsync(yieldId2, _cancellationToken);
    
        // Assert (3)
        await _stack.Received(1).HydrateAsync(Hydrating(yieldId2), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(m1Id), _cancellationToken);
        await _host.Received(1).OnSucceedAsync(CompletionContext, _cancellationToken);
    }

    [Fact]
    public async Task MarshalingDTask_Throws_WhenDTaskIsPending()
    {
        // Arrange
        static async DTask M1()
        {
            DTask task = M2();
            await DTask.Yield();
            _ = task.Status; // Keeps 'task' among the state machine fields
        }

        static async DTask M2()
        {
        }

        var runnable = M1();
        
        // Act
        Func<Task> act = async () => await _sut.StartAsync(runnable, _cancellationToken);
        
        // Assert
        await act.Should().ThrowAsync<MarshalingException>();
    }
    
    [Fact]
    public async Task AwaitsAsyncDTaskInBackground_WhenItCompletesFirst()
    {
        // Arrange (1)
        static async DTask<bool> M1()
        {
            DTask task = await DTask.Run(M2());
            await DTask.Yield();
            await task;
            return task.IsCompleted;
        }
    
        static async DTask M2()
        {
        }
        
        var runnable = M1();
        DAsyncId m1Id = _idFactory.GetTestId(1);
        DAsyncId m2Id = _idFactory.GetTestId(2);
        DAsyncId m2YieldId = _idFactory.GetTestId(3);
        DAsyncId m1YieldId = _idFactory.GetTestId(4);
        
        // Act (1)
        await _sut.StartAsync(runnable, _cancellationToken);
        
        // Assert (1)
        await _stack.Received(1).DehydrateAsync(Dehydrating(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(m2Id, default), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(m2YieldId, m2Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _suspensionHandler.Received(1).OnYieldAsync(m2YieldId, _cancellationToken);
        await _suspensionHandler.Received(1).OnYieldAsync(m1YieldId, _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        
        // Arrange (2)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (2)
        await _sut.ResumeAsync(m2YieldId, _cancellationToken);
        
        // Assert (2)
        await _stack.Received(1).HydrateAsync(Hydrating(m2YieldId), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(m2Id), _cancellationToken);
        await _stack.Received(1).DehydrateCompletedAsync(m2Id, _cancellationToken);
        
        // Arrange (3)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (3)
        await _sut.ResumeAsync(m1YieldId, _cancellationToken);
        
        // Assert (3)
        await _stack.Received(1).HydrateAsync(Hydrating(m1YieldId), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(m1Id), _cancellationToken);
        await _stack.Received(1).LinkAsync(Linking(m2Id, m1Id), _cancellationToken);
        await _host.Received(1).OnSucceedAsync(CompletionContext, true, _cancellationToken);
    }

    [Fact]
    public async Task AwaitsAsyncDTaskInBackground_WhenItCompletesLast()
    {
        // Arrange (1)
        static async DTask<bool> M1()
        {
            DTask task = await DTask.Run(M2());
            await DTask.Yield();
            await task;
            return task.IsCompleted;
        }
    
        static async DTask M2()
        {
        }
        
        var runnable = M1();
        DAsyncId m1Id = _idFactory.GetTestId(1);
        DAsyncId m2Id = _idFactory.GetTestId(2);
        DAsyncId m2YieldId = _idFactory.GetTestId(3);
        DAsyncId m1YieldId = _idFactory.GetTestId(4);
        
        // Act (1)
        await _sut.StartAsync(runnable, _cancellationToken);
        
        // Assert (1)
        await _stack.Received(1).DehydrateAsync(Dehydrating(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(m2Id, default), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(m2YieldId, m2Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _suspensionHandler.Received(1).OnYieldAsync(m2YieldId, _cancellationToken);
        await _suspensionHandler.Received(1).OnYieldAsync(m1YieldId, _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        
        // Arrange (2)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (2)
        await _sut.ResumeAsync(m1YieldId, _cancellationToken);
        
        // Assert (2)
        await _stack.Received(1).HydrateAsync(Hydrating(m1YieldId), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(m1Id), _cancellationToken);
        await _stack.Received(1).LinkAsync(Linking(m2Id, m1Id), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        
        // Arrange (3)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (3)
        await _sut.ResumeAsync(m2YieldId, _cancellationToken);
        
        // Assert (3)
        await _stack.Received(1).HydrateAsync(Hydrating(m2YieldId), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(m2Id), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(m1Id), _cancellationToken);
        await _host.Received(1).OnSucceedAsync(CompletionContext, true, _cancellationToken);
    }

    [Fact]
    public async Task AwaitsAsyncDTaskWithResultInBackground_WhenItCompletesFirst()
    {
        // Arrange (1)
        const int result = 42;
        
        static async DTask<int> M1()
        {
            DTask<int> task = await DTask.Run(M2());
            await DTask.Yield();
            return await task;
        }
    
        static async DTask<int> M2()
        {
            return result;
        }
        
        var runnable = M1();
        DAsyncId m1Id = _idFactory.GetTestId(1);
        DAsyncId m2Id = _idFactory.GetTestId(2);
        DAsyncId m2YieldId = _idFactory.GetTestId(3);
        DAsyncId m1YieldId = _idFactory.GetTestId(4);
        
        // Act (1)
        await _sut.StartAsync(runnable, _cancellationToken);
        
        // Assert (1)
        await _stack.Received(1).DehydrateAsync(Dehydrating(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(m2Id, default), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(m2YieldId, m2Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _suspensionHandler.Received(1).OnYieldAsync(m2YieldId, _cancellationToken);
        await _suspensionHandler.Received(1).OnYieldAsync(m1YieldId, _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        
        // Arrange (2)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (2)
        await _sut.ResumeAsync(m2YieldId, _cancellationToken);
        
        // Assert (2)
        await _stack.Received(1).HydrateAsync(Hydrating(m2YieldId), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(m2Id), _cancellationToken);
        await _stack.Received(1).DehydrateCompletedAsync(m2Id, result, _cancellationToken);
        
        // Arrange (3)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (3)
        await _sut.ResumeAsync(m1YieldId, _cancellationToken);
        
        // Assert (3)
        await _stack.Received(1).HydrateAsync(Hydrating(m1YieldId), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(m1Id), _cancellationToken);
        await _stack.Received(1).LinkAsync(Linking(m2Id, m1Id), _cancellationToken);
        await _host.Received(1).OnSucceedAsync(CompletionContext, result, _cancellationToken);
    }

    [Fact]
    public async Task AwaitsAsyncDTaskWithResultInBackground_WhenItCompletesLast()
    {
        // Arrange (1)
        const int result = 42;
        
        static async DTask<int> M1()
        {
            DTask<int> task = await DTask.Run(M2());
            await DTask.Yield();
            return await task;
        }
    
        static async DTask<int> M2()
        {
            return result;
        }
        
        var runnable = M1();
        DAsyncId m1Id = _idFactory.GetTestId(1);
        DAsyncId m2Id = _idFactory.GetTestId(2);
        DAsyncId m2YieldId = _idFactory.GetTestId(3);
        DAsyncId m1YieldId = _idFactory.GetTestId(4);
        
        // Act (1)
        await _sut.StartAsync(runnable, _cancellationToken);
        
        // Assert (1)
        await _stack.Received(1).DehydrateAsync(Dehydrating(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(m2Id, default), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(m2YieldId, m2Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _suspensionHandler.Received(1).OnYieldAsync(m2YieldId, _cancellationToken);
        await _suspensionHandler.Received(1).OnYieldAsync(m1YieldId, _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        
        // Arrange (2)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (2)
        await _sut.ResumeAsync(m1YieldId, _cancellationToken);
        
        // Assert (2)
        await _stack.Received(1).HydrateAsync(Hydrating(m1YieldId), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(m1Id), _cancellationToken);
        await _stack.Received(1).LinkAsync(Linking(m2Id, m1Id), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        
        // Arrange (3)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (3)
        await _sut.ResumeAsync(m2YieldId, _cancellationToken);
        
        // Assert (3)
        await _stack.Received(1).HydrateAsync(Hydrating(m2YieldId), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(m2Id), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(m1Id), result, _cancellationToken);
        await _host.Received(1).OnSucceedAsync(CompletionContext, result, _cancellationToken);
    }

    [Fact]
    public async Task AwaitsCompletedDTaskInBackground()
    {
        // Arrange
        static async DTask<bool> M1()
        {
            DTask task = await DTask.Run(DTask.CompletedDTask);
            return task.IsCompleted;
        }
        
        var runnable = M1();
        
        // Act
        await _sut.StartAsync(runnable, _cancellationToken);
        
        // Assert
        await _host.Received(1).OnSucceedAsync(CompletionContext, true, _cancellationToken);
    }

    [Fact]
    public async Task MarshalsCompletedDTask()
    {
        // Arrange (1)
        static async DTask<bool> M1()
        {
            DTask task = await DTask.Run(DTask.CompletedDTask);
            await DTask.Yield();
            return task.IsCompleted;
        }
        
        DAsyncId m1Id = _idFactory.GetTestId(1);
        DAsyncId yieldId = _idFactory.GetTestId(3);
        var runnable = M1();
        
        // Act (1)
        await _sut.StartAsync(runnable, _cancellationToken);
        
        // Assert (1)
        await _stack.Received(1).DehydrateAsync(Dehydrating(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(yieldId, m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _suspensionHandler.Received(1).OnYieldAsync(yieldId, _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        
        // Arrange (2)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (2)
        await _sut.ResumeAsync(yieldId, _cancellationToken);
        
        // Assert (2)
        await _stack.Received(1).HydrateAsync(Hydrating(yieldId), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(m1Id), _cancellationToken);
        await _host.Received(1).OnSucceedAsync(CompletionContext, true, _cancellationToken);
    }

    [Fact]
    public async Task AwaitsDelayInBackground()
    {
        // Arrange (1)
        TimeSpan delay = TimeSpan.FromDays(1);
        
        static async DTask M1(TimeSpan delay)
        {
            DTask task = await DTask.Run(DTask.Delay(delay));
            await DTask.Yield();
            await task;
        }
        
        DAsyncId m1Id = _idFactory.GetTestId(1);
        DAsyncId delayId = _idFactory.GetTestId(2);
        DAsyncId yieldId = _idFactory.GetTestId(3);
        var runnable = M1(delay);
        
        // Act (1)
        await _sut.StartAsync(runnable, _cancellationToken);
        
        // Assert (1)
        await _stack.Received(1).DehydrateAsync(Dehydrating(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(yieldId, m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(delayId, default), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _suspensionHandler.Received(1).OnDelayAsync(delayId, delay, _cancellationToken);
        await _suspensionHandler.Received(1).OnYieldAsync(yieldId, _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        
        // Arrange (2)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (2)
        await _sut.ResumeAsync(delayId, _cancellationToken);
        
        // Assert (2)
        await _stack.Received(1).HydrateAsync(Hydrating(delayId), _cancellationToken);
        await _stack.Received(1).DehydrateCompletedAsync(delayId, _cancellationToken);
        
        // Arrange (3)
        _stack.ClearReceivedCalls();
        
        // Act (3)
        await _sut.ResumeAsync(yieldId, _cancellationToken);
        
        // Assert (3)
        await _stack.Received(1).HydrateAsync(Hydrating(yieldId), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(m1Id), _cancellationToken);
        await _stack.Received(1).LinkAsync(Linking(delayId, m1Id), _cancellationToken);
        await _host.Received(1).OnSucceedAsync(CompletionContext, _cancellationToken);
    }

    [Fact]
    public async Task AwaitsWhenAll_WhenThereAreNoTasks()
    {
        // Arrange
        static async DTask M1()
        {
            await DTask.WhenAll();
        }
        
        var runnable = M1();
        
        // Act
        await _sut.StartAsync(runnable, _cancellationToken);

        // Assert
        await _host.Received(1).OnSucceedAsync(CompletionContext, _cancellationToken);
    }

    [Fact]
    public async Task AwaitsWhenAll_WhenDTasksAreCompleted()
    {
        // Arrange
        static async DTask M1()
        {
            await DTask.WhenAll(DTask.CompletedDTask, DTask.CompletedDTask);
        }
        
        var runnable = M1();
        
        // Act
        await _sut.StartAsync(runnable, _cancellationToken);

        // Assert
        await _host.Received(1).OnSucceedAsync(CompletionContext, _cancellationToken);
    }

    [Fact]
    public async Task AwaitsWhenAll_WhenSomeDTasksAreSuspended()
    {
        // Arrange (1)
        TimeSpan delay = TimeSpan.FromDays(1);
        
        static async DTask M1(TimeSpan delay)
        {
            await DTask.WhenAll(DTask.CompletedDTask, DTask.Delay(delay));
        }
        
        DAsyncId m1Id = _idFactory.GetTestId(1);
        DAsyncId whenAllId = _idFactory.GetTestId(2);
        DAsyncId delayId = _idFactory.GetTestId(3);
        var runnable = M1(delay);
        
        // Act (1)
        await _sut.StartAsync(runnable, _cancellationToken);

        // Assert (1)
        await _suspensionHandler.Received(1).OnDelayAsync(delayId, delay, _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(delayId, whenAllId), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(whenAllId, m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        
        // Arrange (2)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (2)
        await _sut.ResumeAsync(delayId, _cancellationToken);
        
        // Assert (2)
        await _stack.Received(1).HydrateAsync(Hydrating(delayId), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(whenAllId), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(m1Id), _cancellationToken);
        await _host.Received(1).OnSucceedAsync(CompletionContext, _cancellationToken);
    }

    [Fact]
    public async Task AwaitsWhenAll_WhenSomeDTasksAreWhenAllOfCompletedDTasks()
    {
        // Arrange
        static async DTask M1()
        {
            await DTask.WhenAll(DTask.CompletedDTask, DTask.WhenAll(DTask.CompletedDTask));
        }
        
        var runnable = M1();
        
        // Act
        await _sut.StartAsync(runnable, _cancellationToken);

        // Assert
        await _host.Received(1).OnSucceedAsync(CompletionContext, _cancellationToken);
    }

    [Fact]
    public async Task AwaitsWhenAll_WhenSomeDTasksAreWhenAllOfSuspendedDTasks()
    {
        // Arrange (1)
        TimeSpan delay = TimeSpan.FromDays(1);
        
        static async DTask M1(TimeSpan delay)
        {
            await DTask.WhenAll(DTask.CompletedDTask, DTask.WhenAll(DTask.Delay(delay)));
        }
        
        DAsyncId m1Id = _idFactory.GetTestId(1);
        DAsyncId whenAll1Id = _idFactory.GetTestId(2);
        DAsyncId whenAll2Id = _idFactory.GetTestId(3);
        DAsyncId delayId = _idFactory.GetTestId(4);
        var runnable = M1(delay);
        
        // Act (1)
        await _sut.StartAsync(runnable, _cancellationToken);

        // Assert (1)
        await _suspensionHandler.Received(1).OnDelayAsync(delayId, delay, _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(delayId, whenAll2Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(whenAll2Id, whenAll1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(whenAll1Id, m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        
        // Arrange (2)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (2)
        await _sut.ResumeAsync(delayId, _cancellationToken);
        
        // Assert (2)
        await _stack.Received(1).HydrateAsync(Hydrating(delayId), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(whenAll2Id), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(whenAll1Id), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(m1Id), _cancellationToken);
        await _host.Received(1).OnSucceedAsync(CompletionContext, _cancellationToken);
    }
    
    [Fact]
    public async Task AwaitsWhenAll()
    {
        // Arrange (1)
        TimeSpan delay = TimeSpan.FromDays(1);
        
        static async DTask M1(TimeSpan delay)
        {
            await DTask.WhenAll(
                M2(),
                DTask.FromResult(string.Empty),
                DTask.CompletedDTask,
                DTask.Delay(delay)
            );
        }
    
        static async DTask M2()
        {
        }
    
        DAsyncId m1Id = _idFactory.GetTestId(1);
        DAsyncId whenAllId = _idFactory.GetTestId(2);
        DAsyncId m2Id = _idFactory.GetTestId(3);
        DAsyncId m2YieldId = _idFactory.GetTestId(4);
        DAsyncId m1DelayId = _idFactory.GetTestId(5);
        DTask task = M1(delay);
    
        // Act (1)
        await _sut.StartAsync(task, _cancellationToken);
        
        // Assert (1)
        await _stack.Received(1).DehydrateAsync(Dehydrating(m2Id, whenAllId), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(m2YieldId, m2Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(m1DelayId, whenAllId), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(whenAllId, m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        await _suspensionHandler.Received(1).OnDelayAsync(m1DelayId, delay, _cancellationToken);
        await _suspensionHandler.Received(1).OnYieldAsync(m2YieldId, _cancellationToken);
        
        // Arrange (2)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (2)
        await _sut.ResumeAsync(m2YieldId, _cancellationToken);
        
        // Assert (2)
        await _stack.Received(1).HydrateAsync(Hydrating(m2YieldId), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(m2Id), _cancellationToken);
        
        // Arrange (3)
        
        // Act (3)
        await _sut.ResumeAsync(m1DelayId, _cancellationToken);
        
        // Assert (3)
    }

    [Fact]
    public async Task RunsWhenAll_WhenThereAreNoTasks()
    {
        // Arrange
        var runnable = DTask.WhenAll();
        
        // Act
        await _sut.StartAsync(runnable, _cancellationToken);

        // Assert
        await _host.Received(1).OnSucceedAsync(CompletionContext, _cancellationToken);
    }

    [Fact]
    public async Task RunsWhenAll_WhenDTasksAreCompleted()
    {
        // Arrange
        var runnable = DTask.WhenAll(DTask.CompletedDTask, DTask.CompletedDTask);
        
        // Act
        await _sut.StartAsync(runnable, _cancellationToken);

        // Assert
        await _host.Received(1).OnSucceedAsync(CompletionContext, _cancellationToken);
    }

    [Fact]
    public async Task RunsWhenAll_WhenSomeDTasksAreSuspended()
    {
        // Arrange (1)
        TimeSpan delay = TimeSpan.FromDays(1);
        
        DAsyncId whenAllId = _idFactory.GetTestId(1);
        DAsyncId delayId = _idFactory.GetTestId(2);
        var runnable = DTask.WhenAll(DTask.CompletedDTask, DTask.Delay(delay));
        
        // Act (1)
        await _sut.StartAsync(runnable, _cancellationToken);

        // Assert (1)
        await _suspensionHandler.Received(1).OnDelayAsync(delayId, delay, _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(delayId, whenAllId), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(whenAllId), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        
        // Arrange (2)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (2)
        await _sut.ResumeAsync(delayId, _cancellationToken);
        
        // Assert (2)
        await _stack.Received(1).HydrateAsync(Hydrating(delayId), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(whenAllId), _cancellationToken);
        await _host.Received(1).OnSucceedAsync(CompletionContext, _cancellationToken);
    }

    [Fact]
    public async Task RunsWhenAll_WhenSomeDTasksAreWhenAllOfCompletedDTasks()
    {
        // Arrange
        var runnable = DTask.WhenAll(DTask.CompletedDTask, DTask.WhenAll(DTask.CompletedDTask));
        
        // Act
        await _sut.StartAsync(runnable, _cancellationToken);

        // Assert
        await _host.Received(1).OnSucceedAsync(CompletionContext, _cancellationToken);
    }

    [Fact]
    public async Task RunsWhenAll_WhenSomeDTasksAreWhenAllOfSuspendedDTasks()
    {
        // Arrange (1)
        TimeSpan delay = TimeSpan.FromDays(1);
        
        DAsyncId whenAll1Id = _idFactory.GetTestId(1);
        DAsyncId whenAll2Id = _idFactory.GetTestId(2);
        DAsyncId delayId = _idFactory.GetTestId(3);
        var runnable = DTask.WhenAll(DTask.CompletedDTask, DTask.WhenAll(DTask.Delay(delay)));
        
        // Act (1)
        await _sut.StartAsync(runnable, _cancellationToken);

        // Assert (1)
        await _suspensionHandler.Received(1).OnDelayAsync(delayId, delay, _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(delayId, whenAll2Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(whenAll2Id, whenAll1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(whenAll1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        
        // Arrange (2)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (2)
        await _sut.ResumeAsync(delayId, _cancellationToken);
        
        // Assert (2)
        await _stack.Received(1).HydrateAsync(Hydrating(delayId), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(whenAll2Id), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(whenAll1Id), _cancellationToken);
        await _host.Received(1).OnSucceedAsync(CompletionContext, _cancellationToken);
    }
    
    [Fact]
    public async Task RunsWhenAll()
    {
        // Arrange (1)
        TimeSpan delay = TimeSpan.FromDays(1);
    
        static async DTask M1()
        {
        }
    
        DAsyncId whenAllId = _idFactory.GetTestId(1);
        DAsyncId m1Id = _idFactory.GetTestId(2);
        DAsyncId m1YieldId = _idFactory.GetTestId(3);
        DAsyncId delayId = _idFactory.GetTestId(4);
        DTask task = DTask.WhenAll(
            M1(),
            DTask.FromResult(string.Empty),
            DTask.CompletedDTask,
            DTask.Delay(delay)
        );
    
        // Act (1)
        await _sut.StartAsync(task, _cancellationToken);
        
        // Assert (1)
        await _stack.Received(1).DehydrateAsync(Dehydrating(m1Id, whenAllId), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(m1YieldId, m1Id), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(delayId, whenAllId), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _stack.Received(1).DehydrateAsync(Dehydrating(whenAllId), ref Arg.Any<Arg.AnyType>(), _cancellationToken);
        await _host.Received(1).OnSuspendAsync(Arg.Any<IDAsyncFlowSuspensionContext>(), _cancellationToken);
        await _suspensionHandler.Received(1).OnDelayAsync(delayId, delay, _cancellationToken);
        await _suspensionHandler.Received(1).OnYieldAsync(m1YieldId, _cancellationToken);
        
        // Arrange (2)
        _stack.ClearReceivedCalls();
        _host.ClearReceivedCalls();
        
        // Act (2)
        await _sut.ResumeAsync(m1YieldId, _cancellationToken);
        
        // Assert (2)
        await _stack.Received(1).HydrateAsync(Hydrating(m1YieldId), _cancellationToken);
        await _stack.Received(1).HydrateAsync(Hydrating(m1Id), _cancellationToken);
        
        // Arrange (3)
        
        // Act (3)
        await _sut.ResumeAsync(delayId, _cancellationToken);
        
        // Assert (3)
    }
    //
    // [Fact]
    // public async Task AwaitsWhenAllOfResult()
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
    // public async Task AwaitsWhenAny()
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

    private static ref IDehydrationContext Dehydrating(DAsyncId id) => ref Arg.Is<IDehydrationContext>(ctx => ctx.Id == id && ctx.ParentId == FakeDAsyncIdFactory.TestRootId);

    private static ref IDehydrationContext Dehydrating(DAsyncId id, DAsyncId parentId) => ref Arg.Is<IDehydrationContext>(ctx => ctx.Id == id && ctx.ParentId == parentId);

    private static ref IHydrationContext Hydrating(DAsyncId id) => ref Arg.Is<IHydrationContext>(ctx => ctx.Id == id);
    
    private static ref ILinkContext Linking(DAsyncId id, DAsyncId parentId) => ref Arg.Is<ILinkContext>(ctx => ctx.Id == id && ctx.ParentId == parentId);
    
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