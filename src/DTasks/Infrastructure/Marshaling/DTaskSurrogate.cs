namespace DTasks.Infrastructure.Marshaling;

internal class DTaskSurrogate
{
    private static readonly Factory s_factory = new();

    public DAsyncId Id { get; set; }

    public DTaskStatus Status { get; set; }

    public Exception? Exception { get; set; }

    internal virtual void Write<T, TAction>(scoped ref TAction action, IDAsyncTypeResolver typeResolver)
        where TAction : ISurrogationAction
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        bool isDTaskAtCompileTime = typeof(DTask).IsAssignableFrom(typeof(T));
        TypeId typeId = isDTaskAtCompileTime
            ? default
            : typeResolver.GetTypeId(typeof(DTask));

        action.SurrogateAs(typeId, this);
    }

    internal virtual DTask ToDTask() => Status switch
    {
        DTaskStatus.Succeeded => DTask.CompletedDTask,
        // TODO: Other statuses
        _ => new DTaskHandle(Id)
    };

    public static DTaskSurrogate Create(DTask task)
    {
        DTaskSurrogate surrogate = task.Accept(s_factory);
        surrogate.Id = DAsyncId.New();
        surrogate.Status = task.Status;
        return surrogate;
    }

    private sealed class Factory : IDTaskVisitor<DTaskSurrogate>
    {
        public DTaskSurrogate Visit(DTask task) => task.Status switch
        {
            DTaskStatus.Pending or
            DTaskStatus.Running or
            DTaskStatus.Suspended or
            DTaskStatus.Succeeded => new DTaskSurrogate(),
            DTaskStatus.Faulted or
            DTaskStatus.Canceled => new DTaskSurrogate { Exception = task.Exception },
            _ => throw new ArgumentException($"Invalid {nameof(DTaskStatus)}: {task.Status}", nameof(task))
        };

        public DTaskSurrogate Visit<TResult>(DTask<TResult> task) => task.Status switch
        {
            DTaskStatus.Pending or
            DTaskStatus.Running or
            DTaskStatus.Suspended => new DTaskSurrogate<TResult>(),
            DTaskStatus.Succeeded => new DTaskSurrogate<TResult> { Result = task.Result },
            DTaskStatus.Faulted or
            DTaskStatus.Canceled => new DTaskSurrogate<TResult> { Exception = task.Exception },
            _ => throw new ArgumentException($"Invalid {nameof(DTaskStatus)}: {task.Status}", nameof(task))
        };
    }
}

internal class DTaskSurrogate<TResult> : DTaskSurrogate
{
    public TResult? Result { get; set; }

    internal override void Write<T, TAction>(ref TAction action, IDAsyncTypeResolver typeResolver)
    {
        bool isDTaskAtCompileTime = typeof(DTask<TResult>).IsAssignableFrom(typeof(T));
        TypeId typeId = isDTaskAtCompileTime
            ? default
            : typeResolver.GetTypeId(typeof(DTask<TResult>));

        action.SurrogateAs(typeId, this);
    }

    internal override DTask ToDTask() => Status switch
    {
        DTaskStatus.Succeeded => DTask.FromResult(Result) as DTask,
        // TODO: Other statuses
        _ => new DTaskHandle<TResult>(Id)
    };
}
