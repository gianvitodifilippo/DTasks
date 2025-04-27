using System.Runtime.CompilerServices;
using DTasks.Infrastructure;
using DTasks.Utils;

namespace DTasks;

internal sealed class WhenAllDTask(IEnumerable<DTask> tasks) : DTask, IDAsyncResultBuilder
{
    private DTaskStatus _status = DTaskStatus.Pending;
    private Exception? _exception;

    public override DTaskStatus Status => _status;

    protected override Exception ExceptionCore
    {
        get
        {
            Assert.NotNull(_exception);
            return _exception;
        }
    }

    protected override void Run(IDAsyncRunner runner) => runner.WhenAll(tasks, this);

    void IDAsyncResultBuilder.SetResult()
    {
        _status = DTaskStatus.Succeeded;
    }

    void IDAsyncResultBuilder.SetException(Exception exception)
    {
        _status = DTaskStatus.Faulted;
        _exception = exception;
    }
}

internal sealed class WhenAllDTask<TResult>(IEnumerable<DTask<TResult>> tasks) : DTask<TResult[]>, IDAsyncResultBuilder<TResult[]>
{
    private DTaskStatus _status = DTaskStatus.Pending;
    private object? _stateObject;

    public override DTaskStatus Status => _status;

    protected override TResult[] ResultCore
    {
        get
        {
            Assert.Is<TResult[]>(_stateObject);

            return Unsafe.As<TResult[]>(_stateObject);
        }
    }

    protected override Exception ExceptionCore
    {
        get
        {
            Assert.Is<Exception>(_stateObject);

            return Unsafe.As<Exception>(_stateObject);
        }
    }

    protected override void Run(IDAsyncRunner runner) => runner.WhenAll(tasks, this);

    void IDAsyncResultBuilder<TResult[]>.SetResult(TResult[] result)
    {
        _status = DTaskStatus.Succeeded;
        _stateObject = result;
    }

    void IDAsyncResultBuilder<TResult[]>.SetException(Exception exception)
    {
        _status = DTaskStatus.Faulted;
        _stateObject = exception;
    }
}
