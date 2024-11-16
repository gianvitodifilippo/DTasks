using DTasks.Hosting;
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
    protected override void Run(IDAsyncFlow flow)
    {
        if (flow is not IDAsyncFlowInternal flowInternal)
            throw new ArgumentException("The provided flow does not support DTask handles.", nameof(flow)); // TODO: Improve message

        switch (_status)
        {
            case DTaskStatus.Pending:
                flowInternal.Handle(id, this);
                break;

            case DTaskStatus.Succeeded:
                flow.Succeed();
                break;

            case DTaskStatus.Faulted:
                flow.Fail(ExceptionCore);
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

    protected override void Run(IDAsyncFlow flow)
    {
        if (flow is not IDAsyncFlowInternal flowInternal)
            throw new ArgumentException("The provided flow does not support DTask handles.", nameof(flow)); // TODO: Improve message

        switch (_status)
        {
            case DTaskStatus.Pending:
                flowInternal.Handle(id, this);
                break;

            case DTaskStatus.Succeeded:
                flow.Succeed(ResultCore);
                break;

            case DTaskStatus.Faulted:
                flow.Fail(ExceptionCore);
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
