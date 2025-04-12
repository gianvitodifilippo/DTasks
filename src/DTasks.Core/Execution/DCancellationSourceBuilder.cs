using System.Diagnostics;
using System.Runtime.ExceptionServices;
using DTasks.Infrastructure;

namespace DTasks.Execution;

internal abstract class DCancellationSourceDAsyncBuilder : IDAsyncResultBuilder<Task>
{
    private DCancellationTokenSource? _result;
    private Exception? _exception;

    public bool IsCompleted { get; private set; }

    protected abstract Task CreateAsync(IDAsyncCancellationManager manager, DCancellationTokenSource source);

    public DCancellationTokenSource GetResult()
    {
        if (!IsCompleted)
            throw new InvalidOperationException($"Attempted to create an instance of {nameof(DCancellationTokenSource)} without awaiting the call.");

        if (_exception is not null)
        {
            ExceptionDispatchInfo.Throw(_exception);
            throw new UnreachableException();
        }

        Debug.Assert(_result is not null);
        return _result;
    }

    public void Continue(IDAsyncRunner runner)
    {
        _result = DCancellationTokenSource.Create(runner.Cancellation);
        runner.Await(CreateAsync(runner.Cancellation, _result), this);
    }

    public void SetResult(Task result)
    {
        IsCompleted = true;
    }

    public void SetException(Exception exception)
    {
        IsCompleted = true;
        _exception = exception;
    }
}