using DTasks.Hosting;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DTasks;

internal sealed class WhenAnyDTask(IEnumerable<DTask> tasks) : DTask<DTask>, IDAsyncResultCallback<DTask>
{
    private DTaskStatus _status = DTaskStatus.Pending;
    private object? _stateObject;

    public override DTaskStatus Status => _status;

    protected override DTask ResultCore
    {
        get
        {
            Debug.Assert(_stateObject is DTask);

            return Unsafe.As<DTask>(_stateObject);
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

    protected override void Run(IDAsyncFlow flow) => flow.WhenAny(tasks, this);

    void IDAsyncResultCallback<DTask>.SetResult(DTask result)
    {
        _status = DTaskStatus.Succeeded;
        _stateObject = result;
    }

    void IDAsyncResultCallback<DTask>.SetException(Exception exception)
    {
        _status = DTaskStatus.Faulted;
        _stateObject = exception;
    }
}

internal sealed class WhenAnyDTask<TResult>(IEnumerable<DTask<TResult>> tasks) : DTask<DTask<TResult>>, IDAsyncResultCallback<DTask<TResult>>
{
    private DTaskStatus _status = DTaskStatus.Pending;
    private object? _stateObject;

    public override DTaskStatus Status => _status;

    protected override DTask<TResult> ResultCore
    {
        get
        {
            Debug.Assert(_stateObject is DTask<TResult>);

            return Unsafe.As<DTask<TResult>>(_stateObject);
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

    protected override void Run(IDAsyncFlow flow) => flow.WhenAny(tasks, this);

    void IDAsyncResultCallback<DTask<TResult>>.SetResult(DTask<TResult> result)
    {
        _status = DTaskStatus.Succeeded;
        _stateObject = result;
    }

    void IDAsyncResultCallback<DTask<TResult>>.SetException(Exception exception)
    {
        _status = DTaskStatus.Faulted;
        _stateObject = exception;
    }
}
