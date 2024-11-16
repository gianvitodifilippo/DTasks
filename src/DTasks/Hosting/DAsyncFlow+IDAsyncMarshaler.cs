using DTasks.Marshaling;
using DTasks.Utils;
using System.Diagnostics;

namespace DTasks.Hosting;

internal partial class DAsyncFlow : IDAsyncMarshaler
{
    private static readonly HandleRunnableTokenConverter _handleRunnableConverter = new();
    private static readonly Func<DTaskToken, DTask> s_toDTask = token => token.ToDTask(); // Using the extension that accepts a Func might allocate in some implementations of IUnmarshalingAction

    bool IDAsyncMarshaler.TryMarshal<T, TAction>(string fieldName, in T value, scoped ref TAction action)
    {
        //if (@object is not null && s_unmarshableTypes.Contains(@object.GetType()))
        //    throw new InvalidOperationException($"'{@object}' cannot be marshaled.");

        if (value is DTask task)
        {
            if (!_tokens.TryGetValue(task, out DTaskToken? taskToken))
            {
                DAsyncId id = DAsyncId.New();
                taskToken = DTaskToken.Create(id, task);
                _tokens.Add(task, taskToken);
            }

            taskToken.Write<T, TAction>(ref action, _typeResolver);
            return true;
        }

        if (value is HandleRunnable handleRunnable)
        {
            Debug.Assert(handleRunnable is not CompletedHandleRunnable);
            handleRunnable.Write<T, TAction>(ref action);
        }

        return _marshaler.TryMarshal(fieldName, in value, ref action);
    }

    bool IDAsyncMarshaler.TryUnmarshal<T, TAction>(string fieldName, TypeId typeId, scoped ref TAction action)
    {
        Type objectType = typeId == default
            ? typeof(T)
            : _typeResolver.GetType(typeId);

        if (objectType == typeof(DTask))
        {
            action.UnmarshalAs(typeof(DTaskToken), s_toDTask);
            return true;
        }

        if (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(DTask<>))
        {
            Type tokenType = typeof(DTaskToken<>).MakeGenericType(objectType.GetGenericArguments());
            action.UnmarshalAs(tokenType, s_toDTask);
            return true;
        }

        if (objectType == typeof(HandleRunnable))
        {
            Type? handleResultType = Consume(ref _handleResultType);
            Assert.NotNull(handleResultType);

            action.UnmarshalAs(handleResultType, _handleRunnableConverter);
            return true;
        }

        return _marshaler.TryUnmarshal<T, TAction>(fieldName, typeId, ref action);
    }

    private sealed class HandleRunnableTokenConverter : ITokenConverter
    {
        public T Convert<TToken, T>(TToken token)
        {
            var runnable = new CompletedHandleRunnable<TToken>(token);
            return (T)(object)runnable;
        }
    }
}
