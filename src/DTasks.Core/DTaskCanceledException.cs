namespace DTasks;

public class DTaskCanceledException : OperationCanceledException
{
    private const string DefaultMessage = "A task was canceled.";

    public DTaskCanceledException()
        : base(DefaultMessage)
    {
    }

    public DTaskCanceledException(string? message)
        : base(message ?? DefaultMessage)
    {
    }

    public DTaskCanceledException(string? message, Exception? innerException)
        : base(message ?? DefaultMessage, innerException)
    {
    }

    public DTaskCanceledException(string? message, Exception? innerException, CancellationToken token)
        : base(message ?? DefaultMessage, innerException, token)
    {
    }

    public DTaskCanceledException(DTask? task) :
        base(DefaultMessage, GetCancellationTokenOrDefault(task))
    {
        if (task is { IsCanceled: false })
            throw new ArgumentException($"Expected a task in the '{nameof(DTaskStatus.Canceled)}' state.", nameof(task));

        Task = task;
    }

    public DTask? Task { get; }

    private static CancellationToken GetCancellationTokenOrDefault(DTask? task) => task is null or { IsCanceled: false }
        ? CancellationToken.None
        : task.CancellationTokenInternal;
}
