using DTasks.Utils;
using System.Diagnostics;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncSurrogator
{
    private static readonly HandleRunnableSurrogateConverter s_handleRunnableConverter = new();

    bool IDAsyncSurrogator.TrySurrogate<T, TAction>(in T value, scoped ref TAction action)
    {
        //if (@object is not null && s_surrogatableTypes.Contains(@object.GetType()))
        //    throw new InvalidOperationException($"'{@object}' cannot be surrogated.");

        if (value is DTask task)
        {
            if (!_surrogates.TryGetValue(task, out DTaskSurrogate? taskSurrogate))
            {
                taskSurrogate = DTaskSurrogate.Create(task);
                _surrogates.Add(task, taskSurrogate);
            }

            taskSurrogate.Write<T, TAction>(ref action, TypeResolver);
            return true;
        }

        if (value is HandleRunnable handleRunnable)
        {
            Debug.Assert(handleRunnable is not CompletedHandleRunnable);
            handleRunnable.Write<T, TAction>(ref action);
            return true;
        }

        return Surrogator.TrySurrogate(in value, ref action);
    }

    bool IDAsyncSurrogator.TryRestore<T, TAction>(TypeId typeId, scoped ref TAction action)
    {
        Type objectType = typeId == default
            ? typeof(T)
            : TypeResolver.GetType(typeId);

        if (objectType == typeof(DTask))
        {
            action.RestoreAs(typeof(DTaskSurrogate), _taskSurrogateConverter);
            return true;
        }

        if (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(DTask<>))
        {
            Type surrogateType = typeof(DTaskSurrogate<>).MakeGenericType(objectType.GetGenericArguments());
            action.RestoreAs(surrogateType, _taskSurrogateConverter);
            return true;
        }

        if (objectType == typeof(HandleRunnable))
        {
            Type? handleResultType = Consume(ref _handleResultType);
            Assert.NotNull(handleResultType);

            action.RestoreAs(handleResultType, s_handleRunnableConverter);
            return true;
        }

        return Surrogator.TryRestore<T, TAction>(typeId, ref action);
    }

    private sealed class DTaskSurrogateConverter(DAsyncFlow flow) : ISurrogateConverter
    {
        public T Convert<TSurrogate, T>(TSurrogate surrogate)
        {
            // TODO: Unify this logic with that of RestorationActionExtensions.FuncSurrogateConverterWrapper
            if (surrogate is not DTaskSurrogate taskSurrogate)
                throw new ArgumentException($"Expected a surrogate of type '{nameof(DTaskSurrogate)}'.", nameof(surrogate));

            if (!flow._tasks.TryGetValue(taskSurrogate.Id, out DTask? task))
            {
                task = taskSurrogate.ToDTask();
                flow._tasks.Add(taskSurrogate.Id, task);
            }

            if (task is not T value)
                throw new InvalidOperationException("Attempted to restore a surrogate to a value of the wrong type.");

            return value;
        }
    }

    private sealed class HandleRunnableSurrogateConverter : ISurrogateConverter
    {
        public T Convert<TSurrogate, T>(TSurrogate surrogate)
        {
            var runnable = new CompletedHandleRunnable<TSurrogate>(surrogate);
            return (T)(object)runnable;
        }
    }
}
