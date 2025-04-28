﻿using System.Runtime.CompilerServices;
using DTasks.Infrastructure;
using DTasks.Utils;

namespace DTasks;

internal sealed class WhenAnyDTask(IEnumerable<DTask> tasks) : DTask<DTask>, IDAsyncResultBuilder<DTask>
{
    private DTaskStatus _status = DTaskStatus.Pending;
    private object? _stateObject;

    public override DTaskStatus Status => _status;

    protected override DTask ResultCore
    {
        get
        {
            Assert.Is<DTask>(_stateObject);

            return Unsafe.As<DTask>(_stateObject);
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

    protected override void Run(IDAsyncRunner runner) => runner.WhenAny(tasks, this);

    void IDAsyncResultBuilder<DTask>.SetResult(DTask result)
    {
        _status = DTaskStatus.Succeeded;
        _stateObject = result;
    }

    void IDAsyncResultBuilder<DTask>.SetException(Exception exception)
    {
        _status = DTaskStatus.Faulted;
        _stateObject = exception;
    }
}

internal sealed class WhenAnyDTask<TResult>(IEnumerable<DTask<TResult>> tasks) : DTask<DTask<TResult>>, IDAsyncResultBuilder<DTask<TResult>>
{
    private DTaskStatus _status = DTaskStatus.Pending;
    private object? _stateObject;

    public override DTaskStatus Status => _status;

    protected override DTask<TResult> ResultCore
    {
        get
        {
            Assert.Is<DTask<TResult>>(_stateObject);

            return Unsafe.As<DTask<TResult>>(_stateObject);
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

    protected override void Run(IDAsyncRunner runner) => runner.WhenAny(tasks, this);

    void IDAsyncResultBuilder<DTask<TResult>>.SetResult(DTask<TResult> result)
    {
        _status = DTaskStatus.Succeeded;
        _stateObject = result;
    }

    void IDAsyncResultBuilder<DTask<TResult>>.SetException(Exception exception)
    {
        _status = DTaskStatus.Faulted;
        _stateObject = exception;
    }
}
