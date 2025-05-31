namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow
{
    private delegate string ErrorMessageProvider(DAsyncFlow flow);

    private static class ErrorMessages
    {
        public static readonly ErrorMessageProvider Default = static self => "Internal failure.";
        public static readonly ErrorMessageProvider OnStart = static self => $"Component '{self._host}' failed while starting a d-async flow.";
        public static readonly ErrorMessageProvider OnSuspend = static self => $"Component '{self._host}' failed while handling suspension of a d-async flow.";
        public static readonly ErrorMessageProvider OnComplete = static self => $"Component '{self._host}' failed while handling completion of a d-async flow.";
        public static readonly ErrorMessageProvider Dehydrate = static self => $"Component '{self._stack}' failed while dehydrating a runnable.";
        public static readonly ErrorMessageProvider Hydrate = static self => $"Component '{self._stack}' failed while hydrating a runnable.";
        public static readonly ErrorMessageProvider Flush = static self => $"Component '{self._stack}' failed while flushing.";
        public static readonly ErrorMessageProvider OnYield = static self => $"Component '{self._suspensionHandler}' failed while executing yield suspension.";
        public static readonly ErrorMessageProvider OnDelay = static self => $"Component '{self._suspensionHandler}' failed while executing delay suspension.";
        public static readonly ErrorMessageProvider SuspensionCallback = static self => $"Component '{self._suspensionCallback}' failed while executing callback.";
    }
}