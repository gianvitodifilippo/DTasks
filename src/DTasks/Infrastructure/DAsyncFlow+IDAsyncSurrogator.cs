using System.Diagnostics.CodeAnalysis;
using DTasks.Infrastructure.Generics;
using DTasks.Infrastructure.Marshaling;
using DTasks.Marshaling;
using DTasks.Utils;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncSurrogator
{
    private static readonly GetSucceededDTaskSurrogatorAction s_getSuccededDTaskSurrogatorAction = new();

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
        
        Type type = typeof(T);
        Type runtimeType = task.GetType();
        
        ITypeContext typeContext = FindBestDTaskTypeContext(runtimeType);
        Type typeContextType = typeContext.Type;
        
        if (!type.IsAssignableFrom(typeContextType))
            throw new MarshalingException($"Type '{type.FullName}' was not registered as a surrogatable type.");

        bool isDTaskAtCompileTime = type == typeContextType;
        TypeId typeId = isDTaskAtCompileTime
            ? default
            : TypeResolver.GetTypeId(typeContextType);

        if (typeContext.Type == typeof(DTask))
        {
            SurrogateDTask(ref marshaller, SucceededDTaskSurrogator.Instance, typeId, task);
            return true;
        }
        
        if (typeContext.IsGeneric && typeContext.GenericType == typeof(DTask<>))
        {
            IDTaskSurrogator succeededDTaskSurrogator = typeContext.ExecuteGeneric(s_getSuccededDTaskSurrogatorAction);
            SurrogateDTask(ref marshaller, succeededDTaskSurrogator, typeId, task);
            return true;
        }

        return false;
    }

    private void SurrogateDTask<TMarshaller>(scoped ref TMarshaller marshaller, IDTaskSurrogator surrogator, TypeId typeId, DTask task)
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
                surrogator.SurrogateSucceeded(ref marshaller, typeId, task);
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

    private bool TryRestoreDTask<T, TUnmarshaller>(Type type, scoped ref TUnmarshaller unmarshaller, [MaybeNullWhen(false)] out T value)
        where TUnmarshaller : IUnmarshaller
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        ITypeContext typeContext = FindBestDTaskTypeContext(type);
        
        if (typeContext.Type == typeof(DTask))
        {
            value = (T)(object)RestoreDTask(ref unmarshaller, SucceededDTaskSurrogator.Instance);
            return true;
        }

        if (typeContext.IsGeneric && typeContext.GenericType == typeof(DTask<>))
        {
            IDTaskSurrogator succeededDTaskSurrogator = typeContext.ExecuteGeneric(s_getSuccededDTaskSurrogatorAction);
            value = (T)(object)RestoreDTask(ref unmarshaller, succeededDTaskSurrogator);
            return true;
        }

        value = default;
        return false;
    }

    private DTask RestoreDTask<TUnmarshaller>(ref TUnmarshaller unmarshaller, IDTaskSurrogator surrogator)
        where TUnmarshaller : IUnmarshaller
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        unmarshaller.BeginArray();
        DTaskStatus status = unmarshaller.ReadItem<DTaskStatus>();

        switch (status)
        {
            case DTaskStatus.Running:
                throw new MarshalingException("Running DTask objects cannot be unmarshaled.");
            
            case DTaskStatus.Pending:
            case DTaskStatus.Suspended:
                DAsyncId id = unmarshaller.ReadItem<DAsyncId>();
                unmarshaller.EndArray();
                
                if (CompletedTasks.TryGetValue(id, out DTask? task))
                    return task;

                DTask handle = surrogator.CreateHandle(id);
                HandleIds.Add(handle, id);
                return handle;
            
            case DTaskStatus.Succeeded:
                return surrogator.RestoreSucceeded(ref unmarshaller);
            
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

    private interface IDTaskSurrogator
    {
        void SurrogateSucceeded<TMarshaller>(scoped ref TMarshaller marshaller, TypeId typeId, DTask task)
            where TMarshaller : IMarshaller
#if NET9_0_OR_GREATER
            , allows ref struct;
#else
            ;
#endif

        DTask RestoreSucceeded<TUnmarshaller>(ref TUnmarshaller unmarshaller)
            where TUnmarshaller : IUnmarshaller
#if NET9_0_OR_GREATER
            , allows ref struct;
#else
            ;
#endif

        DTask CreateHandle(DAsyncId id);
    }

    private sealed class SucceededDTaskSurrogator : IDTaskSurrogator
    {
        public static readonly SucceededDTaskSurrogator Instance = new();
        
        private SucceededDTaskSurrogator()
        {
        }

        public void SurrogateSucceeded<TMarshaller>(scoped ref TMarshaller marshaller, TypeId typeId, DTask task)
            where TMarshaller : IMarshaller
#if NET9_0_OR_GREATER
            , allows ref struct
#endif
        {
            marshaller.BeginArray(typeId, 1);
            marshaller.WriteItem(DTaskStatus.Succeeded);
            marshaller.EndArray();
        }

        public DTask RestoreSucceeded<TUnmarshaller>(ref TUnmarshaller unmarshaller)
            where TUnmarshaller : IUnmarshaller
#if NET9_0_OR_GREATER
            , allows ref struct
#endif
        {
            unmarshaller.EndArray();
            return DTask.CompletedDTask;
        }

        public DTask CreateHandle(DAsyncId id)
        {
            return new DTaskHandle(id);
        }
    }

    private sealed class SucceededDTaskSurrogator<TResult> : IDTaskSurrogator
    {
        public static readonly SucceededDTaskSurrogator<TResult> Instance = new();
        
        private SucceededDTaskSurrogator()
        {
        }

        public void SurrogateSucceeded<TMarshaller>(scoped ref TMarshaller marshaller, TypeId typeId, DTask task)
            where TMarshaller : IMarshaller
#if NET9_0_OR_GREATER
            , allows ref struct
#endif
        {
            DTask<TResult> taskOfResult = Reinterpret.Cast<DTask<TResult>>(task);
            
            marshaller.BeginArray(typeId, 2);
            marshaller.WriteItem(DTaskStatus.Succeeded);
            marshaller.WriteItem(taskOfResult.Result);
            marshaller.EndArray();
        }

        public DTask RestoreSucceeded<TUnmarshaller>(ref TUnmarshaller unmarshaller)
            where TUnmarshaller : IUnmarshaller
#if NET9_0_OR_GREATER
            , allows ref struct
#endif
        {
            TResult result = unmarshaller.ReadItem<TResult>();
            unmarshaller.EndArray();
            return DTask.FromResult(result);
        }

        public DTask CreateHandle(DAsyncId id)
        {
            return new DTaskHandle<TResult>(id);
        }
    }

    private sealed class GetSucceededDTaskSurrogatorAction : IGenericTypeAction<IDTaskSurrogator>
    {
        public IDTaskSurrogator Invoke<TResult>() => SucceededDTaskSurrogator<TResult>.Instance;
    }
}
