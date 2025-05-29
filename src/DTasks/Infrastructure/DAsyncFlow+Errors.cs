namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow
{
    private string HandleOnStartError()
    {
        _resultOrException = null;
        _runnable = null;

        return $"Component '{_host}' failed while starting a d-async flow.";
    }
    
    private string HandleOnSuspendError()
    {
        return $"Component '{_host}' failed while handling suspension of d-async flow.";
    }
    
    private string HandleOnCompleteError()
    {
        return $"Component '{_host}' failed while handling completion of d-async flow.";
    }
    
    private string HandleRedirectError()
    {
        _continuation = null;
        IndirectionErrorHandler? errorHandler = Consume(ref _indirectionErrorHandler);

        errorHandler?.Invoke(this);
        
        return $"Component '{_stack}' failed while dehydrating runnable.";
    }

    private string HandleOnYieldError()
    {
        return $"Component '{_suspensionHandler}' failed while executing yield operation.";
    }

    private string HandleOnDelayError()
    {
        return $"Component '{_suspensionHandler}' failed while executing delay operation.";
    }

    private string HandleSuspensionCallbackError()
    {
        return $"Component '{_suspensionCallback}' failed while executing callback.";
    }

    private string HandleHydrateError()
    {
        return $"Component '{_stack}' failed while hydrating runnable.";
    }
    
    private void CleanUpOnDelay()
    {
        _delay = null;
    }

    private void CleanUpOnCallback()
    {
        _suspensionCallback = null;
    }

    private delegate string InfrastructureErrorHandler(DAsyncFlow flow);
    
    private delegate void IndirectionErrorHandler(DAsyncFlow flow);
    
    private static class ErrorHandlers
    {
        public static readonly InfrastructureErrorHandler Default = static self => "Internal failure.";
        public static readonly InfrastructureErrorHandler OnStart = static self => self.HandleOnStartError();
        public static readonly InfrastructureErrorHandler OnSuspend = static self => self.HandleOnSuspendError();
        public static readonly InfrastructureErrorHandler OnComplete = static self => self.HandleOnCompleteError();
        public static readonly InfrastructureErrorHandler Redirect = static self => self.HandleRedirectError();
        public static readonly InfrastructureErrorHandler OnYield = static self => self.HandleOnYieldError();
        public static readonly InfrastructureErrorHandler OnDelay = static self => self.HandleOnDelayError();
        public static readonly InfrastructureErrorHandler SuspensionCallback = static self => self.HandleSuspensionCallbackError();
        public static readonly InfrastructureErrorHandler Hydrate = static self => self.HandleHydrateError();

        public static class Indirection
        {
            public static readonly IndirectionErrorHandler Delay = static self => self.CleanUpOnDelay();
            public static readonly IndirectionErrorHandler Callback = static self => self.CleanUpOnCallback();
        }
    }
}