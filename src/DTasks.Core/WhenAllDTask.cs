using DTasks.Hosting;
using DTasks.Utils;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DTasks;

internal sealed class WhenAllDTask(IEnumerable<DTask> tasks) : DTask, IDAsyncResultCallback
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

    protected override void Run(IDAsyncFlow flow) => flow.WhenAll(tasks, this);

    void IDAsyncResultCallback.SetResult()
    {
        _status = DTaskStatus.Succeeded;
    }

    void IDAsyncResultCallback.SetException(Exception exception)
    {
        _status = DTaskStatus.Faulted;
        _exception = exception;
    }
}

internal sealed class WhenAllDTask<TResult>(IEnumerable<DTask<TResult>> tasks) : DTask<TResult[]>, IDAsyncResultCallback<TResult[]>
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

    protected override void Run(IDAsyncFlow flow) => flow.WhenAll(tasks, this);

    void IDAsyncResultCallback<TResult[]>.SetResult(TResult[] result)
    {
        _status = DTaskStatus.Succeeded;
        _stateObject = result;
    }

    void IDAsyncResultCallback<TResult[]>.SetException(Exception exception)
    {
        _status = DTaskStatus.Faulted;
        _stateObject = exception;
    }
}
