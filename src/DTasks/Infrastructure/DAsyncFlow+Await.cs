using System.Runtime.CompilerServices;
using DTasks.Execution;
using DTasks.Infrastructure.State;
using DTasks.Marshaling;
using DTasks.Utils;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow
{
    private void AwaitOnStart()
    {
        _state = FlowState.Starting;
        Assign(ref _errorMessageProvider, ErrorMessages.OnStart);
        
        Task task;
        try
        {
            task = _host.OnStartAsync(this, _cancellationToken);
        }
        catch (Exception ex)
        {
            HandleException(ex);
            return;
        }
        
        Await(task);
    }

    private void AwaitOnSuspend()
    {
        _state = FlowState.Terminating;
        Assign(ref _errorMessageProvider, ErrorMessages.OnSuspend);
        
        Task task;
        try
        {
            task = _host.OnSuspendAsync(this, _cancellationToken);
        }
        catch (Exception ex)
        {
            HandleException(ex);
            return;
        }
        
        Await(task);
    }

    private void AwaitOnSucceed()
    {
        _state = FlowState.Terminating;
        Assign(ref _errorMessageProvider, ErrorMessages.OnComplete);
        
        Task task;
        try
        {
            task = _host.OnSucceedAsync(this, _cancellationToken);
        }
        catch (Exception ex)
        {
            HandleException(ex);
            return;
        }
        
        Await(task);
    }

    private void AwaitOnSucceed<TResult>(TResult result)
    {
        _state = FlowState.Terminating;
        Assign(ref _errorMessageProvider, ErrorMessages.OnComplete);
        
        Task task;
        try
        {
            task = _host.OnSucceedAsync(this, result, _cancellationToken);
        }
        catch (Exception ex)
        {
            HandleException(ex);
            return;
        }
        
        Await(task);
    }

    private void AwaitOnFail(Exception exception)
    {
        _state = FlowState.Terminating;
        Assign(ref _errorMessageProvider, ErrorMessages.OnComplete);
        
        Task task;
        try
        {
            task = _host.OnFailAsync(this, exception, _cancellationToken);
        }
        catch (Exception ex)
        {
            HandleException(ex);
            return;
        }
        
        Await(task);
    }

    private void AwaitOnCancel(OperationCanceledException exception)
    {
        _state = FlowState.Terminating;
        Assign(ref _errorMessageProvider, ErrorMessages.OnComplete);
        
        Task task;
        try
        {
            task = _host.OnCancelAsync(this, exception, _cancellationToken);
        }
        catch (Exception ex)
        {
            HandleException(ex);
            return;
        }
        
        Await(task);
    }

    private void AwaitOnYield()
    {
        _state = FlowState.Suspending;
        Assign(ref _errorMessageProvider, ErrorMessages.OnYield);

        Task task;
        try
        {
            task = SuspensionHandler.OnYieldAsync(_id, _cancellationToken);
        }
        catch (Exception ex)
        {
            HandleException(ex);
            return;
        }
        
        Await(task);
    }

    private void AwaitOnDelay()
    {
        _state = FlowState.Suspending;
        Assign(ref _errorMessageProvider, ErrorMessages.OnDelay);
        TimeSpan delay = ConsumeNotNull(ref _delay);

        Task task;
        try
        {
            task = SuspensionHandler.OnDelayAsync(_id, delay, _cancellationToken);
        }
        catch (Exception ex)
        {
            HandleException(ex);
            return;
        }
        
        Await(task);
    }

    private void AwaitCallbackInvoke()
    {
        _state = FlowState.Suspending;
        Assign(ref _errorMessageProvider, ErrorMessages.SuspensionCallback);
        ISuspensionCallback callback = ConsumeNotNull(ref _suspensionCallback);

        Task task;
        try
        {
            task = callback.InvokeAsync(_id, _cancellationToken);
        }
        catch (Exception ex)
        {
            HandleException(ex);
            return;
        }
        
        Await(task);
    }

    private void AwaitDehydrate<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : notnull
    {
        _state = FlowState.Dehydrating;
        Assign(ref _errorMessageProvider, ErrorMessages.Dehydrate);
        
        ValueTask task;
        try
        {
            task = Stack.DehydrateAsync(this, ref stateMachine, _cancellationToken);
        }
        catch (Exception ex)
        {
            HandleException(ex);
            return;
        }
        
        Await(task);
    }

    private void AwaitDehydrateCompleted()
    {
        _state = FlowState.Dehydrating;
        Assign(ref _errorMessageProvider, ErrorMessages.DehydrateCompleted);
        
        ValueTask task;
        try
        {
            task = Stack.DehydrateCompletedAsync(_id, _cancellationToken);
        }
        catch (Exception ex)
        {
            HandleException(ex);
            return;
        }
        
        Await(task);
    }

    private void AwaitDehydrateCompleted<TResult>(TResult result)
    {
        _state = FlowState.Dehydrating;
        Assign(ref _errorMessageProvider, ErrorMessages.DehydrateCompleted);
        
        ValueTask task;
        try
        {
            task = Stack.DehydrateCompletedAsync(_id, result, _cancellationToken);
        }
        catch (Exception ex)
        {
            HandleException(ex);
            return;
        }
        
        Await(task);
    }

    private void AwaitDehydrateCompleted(Exception exception)
    {
        _state = FlowState.Dehydrating;
        Assign(ref _errorMessageProvider, ErrorMessages.DehydrateCompleted);
        
        ValueTask task;
        try
        {
            task = Stack.DehydrateCompletedAsync(_id, exception, _cancellationToken);
        }
        catch (Exception ex)
        {
            HandleException(ex);
            return;
        }
        
        Await(task);
    }

    private void AwaitHydrate()
    {
        _state = FlowState.Hydrating;
        Assign(ref _errorMessageProvider, ErrorMessages.Hydrate);

        ValueTask<DAsyncLink> task;
        try
        {
            task = Stack.HydrateAsync(this, _cancellationToken);
        }
        catch (Exception ex)
        {
            HandleException(ex);
            return;
        }
        
        Await(task);
    }

    private void AwaitHydrate<TResult>(TResult result)
    {
        _state = FlowState.Hydrating;
        Assign(ref _errorMessageProvider, ErrorMessages.Hydrate);

        ValueTask<DAsyncLink> task;
        try
        {
            task = Stack.HydrateAsync(this, result, _cancellationToken);
        }
        catch (Exception ex)
        {
            HandleException(ex);
            return;
        }
        
        Await(task);
    }

    private void AwaitHydrate(Exception exception)
    {
        _state = FlowState.Hydrating;
        Assign(ref _errorMessageProvider, ErrorMessages.Hydrate);

        ValueTask<DAsyncLink> task;
        try
        {
            task = Stack.HydrateAsync(this, exception, _cancellationToken);
        }
        catch (Exception ex)
        {
            HandleException(ex);
            return;
        }
        
        Await(task);
    }

    private void AwaitLink()
    {
        _state = FlowState.Linking;
        Assign(ref _errorMessageProvider, ErrorMessages.Link);

        ValueTask task;
        try
        {
            task = Stack.LinkAsync(this, _cancellationToken);
        }
        catch (Exception ex)
        {
            HandleException(ex);
            return;
        }
        
        Await(task);
    }

    private void AwaitFlush()
    {
        _state = FlowState.Flushing;
        Assign(ref _errorMessageProvider, ErrorMessages.Flush);

        ValueTask task;
        try
        {
            task = Stack.FlushAsync(_cancellationToken);
        }
        catch (Exception ex)
        {
            HandleException(ex);
            return;
        }
        
        Await(task);
    }

    // private void Return()
    // {
    //     _state = FlowState.Returning;
    //     Await(Task.CompletedTask);
    // }

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
        
        _errorMessageProvider = null;
    }

    private void GetVoidValueTaskResult()
    {
        ValueTaskAwaiter voidVta = Consume(ref _voidVta);
        voidVta.GetResult();
        
        _errorMessageProvider = null;
    }

    private DAsyncLink GetLinkValueTaskResult()
    {
        ValueTaskAwaiter<DAsyncLink> linkVta = Consume(ref _linkVta);
        DAsyncLink result = linkVta.GetResult();
        
        _errorMessageProvider = null;
        return result;
    }
}
