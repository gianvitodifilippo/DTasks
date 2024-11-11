using DTasks.Hosting;
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
            Debug.Assert(_exception is not null);
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
            Debug.Assert(_stateObject is TResult[]);

            return Unsafe.As<TResult[]>(_stateObject);
        }
    }

    protected override Exception ExceptionCore
    {
        get
        {
            Debug.Assert(_stateObject is Exception);

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
