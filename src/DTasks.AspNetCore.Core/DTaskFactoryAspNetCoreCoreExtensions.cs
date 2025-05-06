using DTasks.AspNetCore;
using DTasks.AspNetCore.Execution;

namespace DTasks;

public static class DTaskFactoryAspNetCoreCoreExtensions
{
    public static DTask WebSuspend<TCallback>(this DTaskFactory _, ResumptionEndpoint resumptionEndpoint, TCallback callback)
        where TCallback : IWebSuspensionCallback
    {
        return new WebSuspensionDTask<TCallback, VoidDTaskResult>(resumptionEndpoint, callback);
    }

    public static DTask WebSuspend<TCallback>(this DTaskFactory _, TCallback callback)
        where TCallback : IWebSuspensionCallback
    {
        return new WebSuspensionDTask<TCallback, VoidDTaskResult>(null, callback);
    }
    
    public static DTask<TResult> WebSuspend<TCallback, TResult>(this DTaskFactory<TResult> _, ResumptionEndpoint<TResult> resumptionEndpoint, TCallback callback)
        where TCallback : IWebSuspensionCallback
    {
        return new WebSuspensionDTask<TCallback, TResult>(resumptionEndpoint, callback);
    }
    
    public static DTask<TResult> WebSuspend<TCallback, TResult>(this DTaskFactory<TResult> _, TCallback callback)
        where TCallback : IWebSuspensionCallback
    {
        return new WebSuspensionDTask<TCallback, TResult>(null, callback);
    }

    public static DTask WebSuspend(this DTaskFactory _, ResumptionEndpoint resumptionEndpoint, WebSuspensionCallback callback)
    {
        return new DelegateWebSuspensionDTask<VoidDTaskResult>(resumptionEndpoint, callback);
    }

    public static DTask WebSuspend(this DTaskFactory _, WebSuspensionCallback callback)
    {
        return new DelegateWebSuspensionDTask<VoidDTaskResult>(null, callback);
    }
    
    public static DTask<TResult> WebSuspend<TResult>(this DTaskFactory<TResult> _, ResumptionEndpoint<TResult> resumptionEndpoint, WebSuspensionCallback callback)
    {
        return new DelegateWebSuspensionDTask<TResult>(resumptionEndpoint, callback);
    }
    
    public static DTask<TResult> WebSuspend<TResult>(this DTaskFactory<TResult> _, WebSuspensionCallback callback)
    {
        return new DelegateWebSuspensionDTask<TResult>(null, callback);
    }
    
    public static DTask WebSuspend<TState>(this DTaskFactory _, ResumptionEndpoint resumptionEndpoint, TState state, WebSuspensionCallback<TState> callback)
    {
        return new DelegateWebSuspensionDTask<TState, VoidDTaskResult>(resumptionEndpoint, state, callback);
    }
    
    public static DTask WebSuspend<TState>(this DTaskFactory _, TState state, WebSuspensionCallback<TState> callback)
    {
        return new DelegateWebSuspensionDTask<TState, VoidDTaskResult>(null, state, callback);
    }
    
    public static DTask<TResult> WebSuspend<TState, TResult>(this DTaskFactory<TResult> _, ResumptionEndpoint<TResult> resumptionEndpoint, TState state, WebSuspensionCallback<TState> callback)
    {
        return new DelegateWebSuspensionDTask<TState, TResult>(resumptionEndpoint, state, callback);
    }
    
    public static DTask<TResult> WebSuspend<TState, TResult>(this DTaskFactory<TResult> _, TState state, WebSuspensionCallback<TState> callback)
    {
        return new DelegateWebSuspensionDTask<TState, TResult>(null, state, callback);
    }
}
