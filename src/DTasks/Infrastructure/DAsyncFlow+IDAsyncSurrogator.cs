using System.Diagnostics.CodeAnalysis;
using DTasks.Infrastructure.Generics;
using DTasks.Infrastructure.Marshaling;
using DTasks.Marshaling;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncSurrogator
{
    // private static readonly HandleRunnableSurrogateConverter s_handleRunnableConverter = new();

    bool IDAsyncSurrogator.TrySurrogate<T, TMarshaller>(in T value, scoped ref TMarshaller marshaller)
    {
        if (TrySurrogateDTask(in value, ref marshaller))
            return true;
        //
        // if (value is HandleRunnable handleRunnable)
        // {
        //     Debug.Assert(handleRunnable is not CompletedHandleRunnable);
        //     handleRunnable.Write<T, TAction>(ref action);
        //     return true;
        // }

        return Surrogator.TrySurrogate(in value, ref marshaller);
    }

    bool IDAsyncSurrogator.TryRestore<T, TUnmarshaller>(TypeId typeId, scoped ref TUnmarshaller unmarshaller, [MaybeNullWhen(false)] out T value)
    {
        Type type = typeId == default
            ? typeof(T)
            : TypeResolver.GetType(typeId);
        
        if (TryRestoreDTask(type, ref unmarshaller, out value))
            return true;
        
        // if (objectType == typeof(DTask))
        // {
        //     unmarshaller.RestoreAs(typeof(DTaskSurrogate), _taskSurrogateConverter);
        //     return true;
        // }
        //
        // if (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(DTask<>))
        // {
        //     Type surrogateType = typeof(DTaskSurrogate<>).MakeGenericType(objectType.GetGenericArguments());
        //     action.RestoreAs(surrogateType, _taskSurrogateConverter);
        //     return true;
        // }

        // if (objectType == typeof(HandleRunnable))
        // {
        //     Type? handleResultType = Consume(ref _handleResultType);
        //     Assert.NotNull(handleResultType);
        //
        //     action.RestoreAs(handleResultType, s_handleRunnableConverter);
        //     return true;
        // }

        return Surrogator.TryRestore(typeId, ref unmarshaller, out value);
    }

    private bool TrySurrogateDTask<T, TMarshaller>(in T value, scoped ref TMarshaller marshaller)
        where TMarshaller : IMarshaller
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        if (value is not DTask task)
            return false;
        
        Type taskType = task.GetType();
        ITypeContext typeContext = FindBestDTaskTypeContext(taskType);

        if (typeContext.Type == typeof(DTask))
        {
            bool isDTaskAtCompileTime = typeof(T) == typeof(DTask);
            TypeId typeId = isDTaskAtCompileTime
                ? default
                : TypeResolver.GetTypeId(typeof(DTask));
            
            SurrogateDTask(typeId, task, ref marshaller);
            return true;
        }
        
        if (typeContext.IsGeneric && typeContext.GenericType == typeof(DTask<>))
        {
            SurrogateGenericDTask(task, ref marshaller);
            return true;
        }

        return false;
    }

    private void SurrogateDTask<TMarshaller>(TypeId typeId, DTask task, scoped ref TMarshaller marshaller)
        where TMarshaller : IMarshaller
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        switch (task.Status)
        {
            case DTaskStatus.Running:
                throw new MarshalingException("Running DTask objects cannot be marshaled.");
            
            case DTaskStatus.Pending:
            case DTaskStatus.Suspended:
                if (!HandleIds.TryGetValue(task, out DAsyncId id))
                    throw new MarshalingException($"DTask '{task}' cannot be marshaled, as it was not awaited directly or through another awaitable.");
                
                marshaller.BeginArray(typeId, 2);
                marshaller.WriteItem(DTaskStatus.Suspended);
                marshaller.WriteItem(id);
                marshaller.EndArray();
                break;
            
            case DTaskStatus.Succeeded:
                marshaller.BeginArray(typeId, 1);
                marshaller.WriteItem(DTaskStatus.Succeeded);
                marshaller.EndArray();
                break;
            
            case DTaskStatus.Faulted:
                marshaller.BeginArray(typeId, 2);
                marshaller.WriteItem(DTaskStatus.Faulted);
                marshaller.WriteItem(task.ExceptionInternal);
                marshaller.EndArray();
                break;
            
            case DTaskStatus.Canceled:
                throw new NotImplementedException();
            
            default:
                throw new InvalidOperationException($"Unsupported DTask status: {task.Status}.");
        }
    }

    private void SurrogateGenericDTask<TMarshaller>(DTask task, scoped ref TMarshaller marshaller)
        where TMarshaller : IMarshaller
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        throw new NotImplementedException();
    }

    private bool TryRestoreDTask<T, TUnmarshaller>(Type type, scoped ref TUnmarshaller unmarshaller, [MaybeNullWhen(false)] out T value)
        where TUnmarshaller : IUnmarshaller
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        ITypeContext typeContext = FindBestDTaskTypeContext(type);
        
        if (typeContext.Type == typeof(DTask))
        {
            value = (T)(object)RestoreDTask(ref unmarshaller);
            return true;
        }

        if (typeContext.IsGeneric && typeContext.GenericType == typeof(DTask<>))
        {
            value = (T)(object)RestoreGenericDTask(ref unmarshaller);
            return true;
        }

        value = default;
        return false;
    }

    private DTask RestoreDTask<TUnmarshaller>(ref TUnmarshaller unmarshaller)
        where TUnmarshaller : IUnmarshaller
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        unmarshaller.BeginArray();
        DTaskStatus status = unmarshaller.ReadItem<DTaskStatus>();

        switch (status)
        {
            case DTaskStatus.Pending:
            case DTaskStatus.Running:
                throw new MarshalingException("Pending and running DTask objects cannot be unmarshaled.");
            
            case DTaskStatus.Suspended:
                DAsyncId id = unmarshaller.ReadItem<DAsyncId>();
                unmarshaller.EndArray();
                
                if (CompletedTasks.TryGetValue(id, out DTask? task))
                    return task;
                
                DTaskHandle handle = new(id);
                HandleIds.Add(handle, id);
                return handle;
            
            case DTaskStatus.Succeeded:
                unmarshaller.EndArray();
                return DTask.CompletedDTask;
            
            case DTaskStatus.Faulted:
                Exception exception = unmarshaller.ReadItem<Exception>();
                unmarshaller.EndArray();
                return DTask.FromException(exception);
            
            case DTaskStatus.Canceled:
                throw new NotImplementedException();
            
            default:
                throw new InvalidOperationException($"Unsupported DTask status: '{status}'.");
        }
    }

    private static DTask RestoreGenericDTask<TUnmarshaller>(ref TUnmarshaller unmarshaller)
        where TUnmarshaller : IUnmarshaller
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        throw new NotImplementedException();
    }

    // private sealed class DTaskSurrogateConverter(DAsyncFlow flow) : ISurrogateConverter
    // {
    //     public T Convert<TSurrogate, T>(TSurrogate surrogate)
    //     {
    //         if (surrogate is not DTaskSurrogate taskSurrogate)
    //             throw new ArgumentException($"Expected a surrogate of type '{nameof(DTaskSurrogate)}'.", nameof(surrogate));
    //
    //         // if (!flow._tasks.TryGetValue(taskSurrogate.Id, out DTask? task))
    //         // {
    //         //     task = taskSurrogate.ToDTask();
    //         //     flow._tasks.Add(taskSurrogate.Id, task);
    //         // }
    //         DTask task = taskSurrogate.ToDTask();
    //
    //         if (task is not T value)
    //             throw new InvalidOperationException("Attempted to restore a surrogate to a value of the wrong type.");
    //
    //         return value;
    //     }
    // }

    private ITypeContext FindBestDTaskTypeContext(Type taskType)
    {
        // TODO: Create a custom component for this logic

        Type? type = taskType;
        while (type is not null)
        {
            foreach (ITypeContext context in _infrastructure.RootScope.SurrogatableTypeContexts)
            {
                if (context.Type == type)
                    return context;
            }
            
            type = type.BaseType;
        }

        throw new InvalidOperationException($"Expected '{nameof(DTask)}' to have been registered as a surrogatable type.");
    }
}
