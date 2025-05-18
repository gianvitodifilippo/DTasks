using DTasks.Execution;

namespace DTasks;

public static class DTaskFactoryExtensions
{
    public static DTask Suspend<TCallback>(this DTaskFactory _, TCallback callback)
        where TCallback : ISuspensionCallback
    {
        return new SuspensionDTask<TCallback, VoidDTaskResult>(callback);
    }

    public static DTask<TResult> Suspend<TCallback, TResult>(this DTaskFactory<TResult> _, TCallback callback)
        where TCallback : ISuspensionCallback
    {
        return new SuspensionDTask<TCallback, TResult>(callback);
    }

    public static DTask Suspend(this DTaskFactory _, SuspensionCallback callback)
    {
        return new DelegateSuspensionDTask<VoidDTaskResult>(callback);
    }

    public static DTask Suspend<TState>(this DTaskFactory _, TState state, SuspensionCallback<TState> callback)
    {
        return new DelegateSuspensionDTask<TState, VoidDTaskResult>(state, callback);
    }

    public static DTask<TResult> Suspend<TResult>(this DTaskFactory<TResult> _, SuspensionCallback callback)
    {
        return new DelegateSuspensionDTask<TResult>(callback);
    }

    public static DTask<TResult> Suspend<TResult, TState>(this DTaskFactory<TResult> _, TState state, SuspensionCallback<TState> callback)
    {
        return new DelegateSuspensionDTask<TState, TResult>(state, callback);
    }
}
