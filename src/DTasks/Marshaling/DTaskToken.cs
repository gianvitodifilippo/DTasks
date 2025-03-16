using DTasks.Hosting;

namespace DTasks.Marshaling;

internal class DTaskToken
{
    private static readonly Factory s_factory = new();

    public DAsyncId Id { get; set; }

    public DTaskStatus Status { get; set; }

    public Exception? Exception { get; set; }

    internal virtual void Write<T, TAction>(scoped ref TAction action, ITypeResolver typeResolver)
        where TAction : IMarshalingAction
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        TypeId typeId = typeof(DTask).IsAssignableFrom(typeof(T))
            ? default
            : typeResolver.GetTypeId(typeof(DTask));

        action.MarshalAs(typeId, this);
    }

    internal virtual DTask ToDTask() => Status switch
    {
        DTaskStatus.Succeeded => DTask.CompletedDTask,
        // TODO: Other statuses
        _ => new DTaskHandle(Id)
    };

    public static DTaskToken Create(DAsyncId id, DTask task)
    {
        DTaskToken token = task.Accept(s_factory);
        token.Id = id;
        token.Status = task.Status;
        return token;
    }

    private sealed class Factory : IDTaskVisitor<DTaskToken>
    {
        public DTaskToken Visit(DTask task) => task.Status switch
        {
            DTaskStatus.Pending or
            DTaskStatus.Running or
            DTaskStatus.Suspended or
            DTaskStatus.Succeeded => new DTaskToken(),
            DTaskStatus.Faulted or
            DTaskStatus.Canceled => new DTaskToken { Exception = task.Exception },
            _ => throw new ArgumentException($"Invalid {nameof(DTaskStatus)}: {task.Status}", nameof(task))
        };

        public DTaskToken Visit<TResult>(DTask<TResult> task) => task.Status switch
        {
            DTaskStatus.Pending or
            DTaskStatus.Running or
            DTaskStatus.Suspended => new DTaskToken<TResult>(),
            DTaskStatus.Succeeded => new DTaskToken<TResult> { Result = task.Result },
            DTaskStatus.Faulted or
            DTaskStatus.Canceled => new DTaskToken<TResult> { Exception = task.Exception },
            _ => throw new ArgumentException($"Invalid {nameof(DTaskStatus)}: {task.Status}", nameof(task))
        };
    }
}

internal class DTaskToken<TResult> : DTaskToken
{
    public TResult? Result { get; set; }

    internal override void Write<T, TAction>(ref TAction action, ITypeResolver typeResolver)
    {
        TypeId typeId = typeof(DTask<TResult>).IsAssignableFrom(typeof(T))
            ? default
            : typeResolver.GetTypeId(typeof(DTask<TResult>));

        action.MarshalAs(typeId, this);
    }

    internal override DTask ToDTask() => Status switch
    {
        DTaskStatus.Succeeded => DTask.FromResult(Result) as DTask,
        // TODO: Other statuses
        _ => new DTaskHandle<TResult>(Id)
    };
}
