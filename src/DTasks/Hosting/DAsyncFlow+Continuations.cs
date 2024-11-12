using System.Diagnostics;

namespace DTasks.Hosting;

internal partial class DAsyncFlow
{
    private static void StartContinuation(DAsyncFlow self)
    {
        IDAsyncStateMachine? stateMachine = self.Consume(ref self._stateMachine);

        Debug.Assert(stateMachine is not null);
        self.Start(stateMachine);
    }

    private static void YieldContinuation(DAsyncFlow self)
    {
        self.Yield();
    }

    private static void DelayContinuation(DAsyncFlow self)
    {
        TimeSpan delay = self.Consume(ref self._delay);

        self.Delay(delay);
    }

    private static void CallbackContinuation(DAsyncFlow self)
    {
        ISuspensionCallback? callback = self.Consume(ref self._callback);

        Debug.Assert(callback is not null);
        self.Callback(callback);
    }

    private static void WhenAllContinuation(DAsyncFlow self)
    {
        IEnumerable<IDAsyncRunnable>? aggregateBranches = self.Consume(ref self._aggregateBranches);
        object? resultCallback = self.Consume(ref self._resultCallback);

        Debug.Assert(aggregateBranches is not null);
        Debug.Assert(resultCallback is IDAsyncResultCallback);
        self.WhenAll(aggregateBranches, (IDAsyncResultCallback)resultCallback);
    }

    private static void WhenAllContinuation<TResult>(DAsyncFlow self)
    {
        IEnumerable<IDAsyncRunnable>? aggregateBranches = self.Consume(ref self._aggregateBranches);
        object? resultCallback = self.Consume(ref self._resultCallback);

        Debug.Assert(aggregateBranches is not null);
        Debug.Assert(resultCallback is IDAsyncResultCallback<TResult[]>);
        self.WhenAll(aggregateBranches, (IDAsyncResultCallback<TResult[]>)resultCallback);
    }

    private static void WhenAnyContinuation(DAsyncFlow self)
    {
        IEnumerable<IDAsyncRunnable>? aggregateBranches = self.Consume(ref self._aggregateBranches);
        object? resultCallback = self.Consume(ref self._resultCallback);

        Debug.Assert(aggregateBranches is not null);
        Debug.Assert(resultCallback is IDAsyncResultCallback<DTask>);
        self.WhenAny(aggregateBranches, (IDAsyncResultCallback<DTask>)resultCallback);
    }

    private static void WhenAnyContinuation<TResult>(DAsyncFlow self)
    {
        IEnumerable<IDAsyncRunnable>? aggregateBranches = self.Consume(ref self._aggregateBranches);
        object? resultCallback = self.Consume(ref self._resultCallback);

        Debug.Assert(aggregateBranches is not null);
        Debug.Assert(resultCallback is IDAsyncResultCallback<DTask<TResult>>);
        self.WhenAny(aggregateBranches, (IDAsyncResultCallback<DTask<TResult>>)resultCallback);
    }

    private static void RunContinuation(DAsyncFlow self)
    {
        IDAsyncRunnable? backgroundRunnable = self.Consume(ref self._backgroundRunnable);
        object? resultCallback = self.Consume(ref self._resultCallback);

        Debug.Assert(backgroundRunnable is not null);
        Debug.Assert(resultCallback is IDAsyncResultCallback<DTask>);
        self.Run(backgroundRunnable, (IDAsyncResultCallback<DTask>)resultCallback);
    }

    private static void RunContinuation<TResult>(DAsyncFlow self)
    {
        IDAsyncRunnable? backgroundRunnable = self.Consume(ref self._backgroundRunnable);
        object? resultCallback = self.Consume(ref self._resultCallback);

        Debug.Assert(backgroundRunnable is not null);
        Debug.Assert(resultCallback is IDAsyncResultCallback<DTask<TResult>>);
        self.Run(backgroundRunnable, (IDAsyncResultCallback<DTask<TResult>>)resultCallback);
    }

    private static void YieldIndirectionContinuation(DAsyncFlow self)
    {
        self.RunIndirection(YieldContinuation);
    }

    private static void DelayIndirectionContinuation(DAsyncFlow self)
    {
        self.RunIndirection(DelayContinuation);
    }

    private static void CallbackIndirectionContinuation(DAsyncFlow self)
    {
        self.RunIndirection(CallbackContinuation);
    }

    private delegate void FlowContinuation(DAsyncFlow self);
}
