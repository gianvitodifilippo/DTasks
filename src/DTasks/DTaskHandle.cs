using DTasks.Infrastructure;
using DTasks.Utils;
using System.Diagnostics;

namespace DTasks;

internal sealed class DTaskHandle(DAsyncId id) : DTask, IDAsyncResultBuilder
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
    protected override void Run(IDAsyncRunner runner)
    {
        if (runner is not IDAsyncRunnerInternal runnerInternal)
            throw new ArgumentException("The provided runner does not support DTask handles.", nameof(runner)); // TODO: Improve message

        switch (_status)
        {
            case DTaskStatus.Pending:
                runnerInternal.Handle(id, this);
                break;

            case DTaskStatus.Succeeded:
                runner.Succeed();
                break;

            case DTaskStatus.Faulted:
                runner.Fail(ExceptionCore);
                break;

            default:
                Debug.Fail("Invalid status.");
                break;
        }
    }

    void IDAsyncResultBuilder.SetResult()
    {
        Debug.Assert(_status is DTaskStatus.Pending);

        _status = DTaskStatus.Succeeded;
    }

    void IDAsyncResultBuilder.SetException(Exception exception)
    {
        Debug.Assert(_status is DTaskStatus.Pending);

        _status = DTaskStatus.Faulted;
        _exception = exception;
    }
}

internal sealed class DTaskHandle<TResult>(DAsyncId id) : DTask<TResult>, IDAsyncResultBuilder<TResult>
{
    private DTaskStatus _status = DTaskStatus.Pending;
    private TResult? _result;
    private Exception? _exception;

    public override DTaskStatus Status => _status;

    protected override TResult ResultCore => _result!;

    protected override Exception ExceptionCore
    {
        get
        {
            Assert.NotNull(_exception);
            return _exception;
        }
    }

    protected override void Run(IDAsyncRunner runner)
    {
        if (runner is not IDAsyncRunnerInternal runnerInternal)
            throw new ArgumentException("The provided runner does not support DTask handles.", nameof(runner)); // TODO: Improve message

        switch (_status)
        {
            case DTaskStatus.Pending:
                runnerInternal.Handle(id, this);
                break;

            case DTaskStatus.Succeeded:
                runner.Succeed(ResultCore);
                break;

            case DTaskStatus.Faulted:
                runner.Fail(ExceptionCore);
                break;

            default:
                Debug.Fail("Invalid status.");
                break;
        }
    }

    void IDAsyncResultBuilder<TResult>.SetResult(TResult result)
    {
        Debug.Assert(_status is DTaskStatus.Pending);

        _status = DTaskStatus.Succeeded;
        _result = result;
    }

    void IDAsyncResultBuilder<TResult>.SetException(Exception exception)
    {
        Debug.Assert(_status is DTaskStatus.Pending);
        
        _status = DTaskStatus.Faulted;
        _exception = exception;
    }
}
