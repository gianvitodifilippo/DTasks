using DTasks.Infrastructure;

namespace DTasks;

public static class DTaskFactoryExtensions
{
    public static DTask Callback<TCallback>(this DTaskFactory _, TCallback callback)
        where TCallback : ISuspensionCallback
    {
        return new CallbackDTask<VoidDTaskResult, TCallback>(callback);
    }

    public static DTask Callback<TState, TCallback>(this DTaskFactory _, TState state, TCallback callback)
        where TCallback : ISuspensionCallback<TState>
    {
        return new CallbackDTask<VoidDTaskResult, TState, TCallback>(state, callback);
    }

    public static DTask<TResult> Callback<TResult, TCallback>(this DTaskFactory<TResult> _, TCallback callback)
        where TCallback : ISuspensionCallback
    {
        return new CallbackDTask<TResult, TCallback>(callback);
    }

    public static DTask<TResult> Callback<TResult, TState, TCallback>(this DTaskFactory<TResult> _, TState state, TCallback callback)
        where TCallback : ISuspensionCallback<TState>
    {
        return new CallbackDTask<TResult, TState, TCallback>(state, callback);
    }

    public static DTask Callback(this DTaskFactory _, SuspensionCallback callback)
    {
        return new DelegateCallbackDTask<VoidDTaskResult>(callback);
    }

    public static DTask Callback<TState>(this DTaskFactory _, TState state, SuspensionCallback<TState> callback)
    {
        return new DelegateCallbackDTask<VoidDTaskResult, TState>(state, callback);
    }

    public static DTask<TResult> Callback<TResult>(this DTaskFactory<TResult> _, SuspensionCallback callback)
    {
        return new DelegateCallbackDTask<TResult>(callback);
    }

    public static DTask<TResult> Callback<TResult, TState>(this DTaskFactory<TResult> _, TState state, SuspensionCallback<TState> callback)
    {
        return new DelegateCallbackDTask<TResult, TState>(state, callback);
    }
}
