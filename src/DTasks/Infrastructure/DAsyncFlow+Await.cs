using System.Diagnostics;
using System.Runtime.CompilerServices;
using DTasks.Execution;
using DTasks.Infrastructure.State;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow
{
    private void AwaitOnStart()
    {
        _state = FlowState.Starting;
        Assign(ref _errorHandler, ErrorHandlers.OnStart);
        
        Task task;
        try
        {
            task = _host.OnStartAsync(this, _cancellationToken);
        }
        catch (Exception ex)
        {
            SetInfrastructureException(ex);
            return;
        }
        
        Await(task);
    }

    private void AwaitOnSuspend()
    {
        _state = FlowState.Returning;
        Assign(ref _errorHandler, ErrorHandlers.OnSuspend);
        
        Task task;
        try
        {
            task = _host.OnSuspendAsync(this, _cancellationToken);
        }
        catch (Exception ex)
        {
            SetInfrastructureException(ex);
            return;
        }
        
        Await(task);
    }

    private void AwaitOnSucceed()
    {
        _state = FlowState.Returning;
        Assign(ref _errorHandler, ErrorHandlers.OnComplete);
        
        Task task;
        try
        {
            task = _host.OnSucceedAsync(this, _cancellationToken);
        }
        catch (Exception ex)
        {
            SetInfrastructureException(ex);
            return;
        }
        
        Await(task);
    }

    private void AwaitOnSucceed<TResult>(TResult result)
    {
        _state = FlowState.Returning;
        Assign(ref _errorHandler, ErrorHandlers.OnComplete);
        
        Task task;
        try
        {
            task = _host.OnSucceedAsync(this, result, _cancellationToken);
        }
        catch (Exception ex)
        {
            SetInfrastructureException(ex);
            return;
        }
        
        Await(task);
    }

    private void AwaitOnFail(Exception exception)
    {
        _state = FlowState.Returning;
        Assign(ref _errorHandler, ErrorHandlers.OnComplete);
        
        Task task;
        try
        {
            task = _host.OnFailAsync(this, exception, _cancellationToken);
        }
        catch (Exception ex)
        {
            SetInfrastructureException(ex);
            return;
        }
        
        Await(task);
    }

    private void AwaitOnCancel(OperationCanceledException exception)
    {
        _state = FlowState.Returning;
        Assign(ref _errorHandler, ErrorHandlers.OnComplete);
        
        Task task;
        try
        {
            task = _host.OnCancelAsync(this, exception, _cancellationToken);
        }
        catch (Exception ex)
        {
            SetInfrastructureException(ex);
            return;
        }
        
        Await(task);
    }

    private void AwaitRedirect(IndirectionContinuation continuation, IndirectionErrorHandler? errorHandler)
    {
        _state = FlowState.Dehydrating;
        Assign(ref _continuation, continuation);
        Assign(ref _indirectionErrorHandler, errorHandler);
        Assign(ref _errorHandler, ErrorHandlers.Redirect);
        Assign(ref _suspendingAwaiterOrType, typeof(IndirectionAwaiter));
        _parentId = _id;
        _id = DAsyncId.New();
        IndirectionStateMachine stateMachine = default;
        
        ValueTask task;
        try
        {
            task = Stack.DehydrateAsync(this, ref stateMachine, _cancellationToken);
        }
        catch (Exception ex)
        {
            SetInfrastructureException(ex);
            return;
        }
        
        Await(task);
    }

    private void AwaitOnYield()
    {
        _state = FlowState.Suspending;
        Assign(ref _errorHandler, ErrorHandlers.OnYield);

        Task task;
        try
        {
            task = SuspensionHandler.OnYieldAsync(_id, _cancellationToken);
        }
        catch (Exception ex)
        {
            SetInfrastructureException(ex);
            return;
        }
        
        Await(task);
    }

    private void AwaitOnDelay()
    {
        _state = FlowState.Suspending;
        Assign(ref _errorHandler, ErrorHandlers.OnDelay);
        TimeSpan delay = ConsumeNotNull(ref _delay);

        Task task;
        try
        {
            task = SuspensionHandler.OnDelayAsync(_id, delay, _cancellationToken);
        }
        catch (Exception ex)
        {
            SetInfrastructureException(ex);
            return;
        }
        
        Await(task);
    }

    private void AwaitOnCallback()
    {
        _state = FlowState.Suspending;
        Assign(ref _errorHandler, ErrorHandlers.SuspensionCallback);
        ISuspensionCallback callback = ConsumeNotNull(ref _suspensionCallback);

        Task task;
        try
        {
            task = callback.InvokeAsync(_id, _cancellationToken);
        }
        catch (Exception ex)
        {
            SetInfrastructureException(ex);
            return;
        }
        
        Await(task);
    }

    private void AwaitHydrate(DAsyncId id)
    {
        _state = FlowState.Hydrating;
        Assign(ref _errorHandler, ErrorHandlers.Hydrate);
        _id = id;

        ValueTask<DAsyncLink> task;
        try
        {
            task = Stack.HydrateAsync(this, _cancellationToken);
        }
        catch (Exception ex)
        {
            SetInfrastructureException(ex);
            return;
        }
        
        Await(task);
    }

    private void AwaitHydrate<TResult>(DAsyncId id, TResult result)
    {
        _state = FlowState.Hydrating;
        Assign(ref _errorHandler, ErrorHandlers.Hydrate);
        _id = id;

        ValueTask<DAsyncLink> task;
        try
        {
            task = Stack.HydrateAsync(this, result, _cancellationToken);
        }
        catch (Exception ex)
        {
            SetInfrastructureException(ex);
            return;
        }
        
        Await(task);
    }

    private void AwaitHydrate(DAsyncId id, Exception exception)
    {
        _state = FlowState.Hydrating;
        Assign(ref _errorHandler, ErrorHandlers.Hydrate);
        _id = id;

        ValueTask<DAsyncLink> task;
        try
        {
            task = Stack.HydrateAsync(this, exception, _cancellationToken);
        }
        catch (Exception ex)
        {
            SetInfrastructureException(ex);
            return;
        }
        
        Await(task);
    }

    private void Return()
    {
        _state = FlowState.Returning;
        Await(Task.CompletedTask);
    }

    private void Await(Task task)
    {
        var self = this;
        _voidTa = task.GetAwaiter();
        _builder.AwaitUnsafeOnCompleted(ref _voidTa, ref self);
    }

    private void Await(ValueTask task)
    {
        var self = this;
        _voidVta = task.GetAwaiter();
        _builder.AwaitUnsafeOnCompleted(ref _voidVta, ref self);
    }

    private void Await(ValueTask<DAsyncLink> task)
    {
        var self = this;
        _linkVta = task.GetAwaiter();
        _builder.AwaitUnsafeOnCompleted(ref _linkVta, ref self);
    }

    private void GetVoidTaskResult()
    {
        TaskAwaiter voidTa = Consume(ref _voidTa);
        voidTa.GetResult();
        
        _errorHandler = null;
        _indirectionErrorHandler = null;
    }

    private void GetVoidValueTaskResult()
    {
        ValueTaskAwaiter voidVta = Consume(ref _voidVta);
        voidVta.GetResult();
        
        _errorHandler = null;
    }

    private DAsyncLink GetLinkValueTaskResult()
    {
        ValueTaskAwaiter<DAsyncLink> linkVta = Consume(ref _linkVta);
        DAsyncLink result = linkVta.GetResult();
        
        _errorHandler = null;
        return result;
    }
}
