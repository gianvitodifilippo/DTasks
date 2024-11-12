using DTasks.Utils;
using System.Runtime.CompilerServices;

namespace DTasks.Hosting;

internal partial class DAsyncFlow
{
    private static class Continuations
    {
        public static void Return(DAsyncFlow self)
        {
            self.Await(Task.CompletedTask, FlowState.Returning);
        }

        public static void Start(DAsyncFlow self)
        {
            IDAsyncStateMachine? stateMachine = self.Consume(ref self._stateMachine);

            Assert.NotNull(stateMachine);
            self.Start(stateMachine);
        }

        public static void Yield(DAsyncFlow self)
        {
            self.Yield();
        }

        public static void Delay(DAsyncFlow self)
        {
            TimeSpan? delay = self.Consume(ref self._delay);

            Assert.NotNull(delay);
            self.Delay(delay.Value);
        }

        public static void Callback(DAsyncFlow self)
        {
            ISuspensionCallback? callback = self.Consume(ref self._callback);

            Assert.NotNull(callback);
            self.Callback(callback);
        }

        public static void WhenAll(DAsyncFlow self)
        {
            IEnumerable<IDAsyncRunnable>? aggregateBranches = self.Consume(ref self._aggregateBranches);
            object? resultCallback = self.Consume(ref self._resultCallback);

            Assert.NotNull(aggregateBranches);
            Assert.Is<IDAsyncResultCallback>(resultCallback);
            self.Await(self.WhenAllAsync(aggregateBranches, Unsafe.As<IDAsyncResultCallback>(resultCallback)), FlowState.WhenAll);
        }

        public static void WhenAll<TResult>(DAsyncFlow self)
        {
            IEnumerable<IDAsyncRunnable>? aggregateBranches = self.Consume(ref self._aggregateBranches);
            object? resultCallback = self.Consume(ref self._resultCallback);

            Assert.NotNull(aggregateBranches);
            Assert.Is<IDAsyncResultCallback<TResult[]>>(resultCallback);
            self.WhenAll(aggregateBranches, Unsafe.As<IDAsyncResultCallback<TResult[]>>(resultCallback));
        }

        public static void WhenAny(DAsyncFlow self)
        {
            IEnumerable<IDAsyncRunnable>? aggregateBranches = self.Consume(ref self._aggregateBranches);
            object? resultCallback = self.Consume(ref self._resultCallback);

            Assert.NotNull(aggregateBranches);
            Assert.Is<IDAsyncResultCallback<DTask>>(resultCallback);
            self.WhenAny(aggregateBranches, Unsafe.As<IDAsyncResultCallback<DTask>>(resultCallback));
        }

        public static void WhenAny<TResult>(DAsyncFlow self)
        {
            IEnumerable<IDAsyncRunnable>? aggregateBranches = self.Consume(ref self._aggregateBranches);
            object? resultCallback = self.Consume(ref self._resultCallback);

            Assert.NotNull(aggregateBranches);
            Assert.Is<IDAsyncResultCallback<DTask<TResult>>>(resultCallback);
            self.WhenAny(aggregateBranches, Unsafe.As<IDAsyncResultCallback<DTask<TResult>>>(resultCallback));
        }

        public static void Background(DAsyncFlow self)
        {
            IDAsyncRunnable? backgroundRunnable = self.Consume(ref self._backgroundRunnable);
            object? resultCallback = self.Consume(ref self._resultCallback);

            Assert.NotNull(backgroundRunnable);
            Assert.Is<IDAsyncResultCallback<DTask>>(resultCallback);
            self.Background(backgroundRunnable, Unsafe.As<IDAsyncResultCallback<DTask>>(resultCallback));
        }

        public static void Background<TResult>(DAsyncFlow self)
        {
            IDAsyncRunnable? backgroundRunnable = self.Consume(ref self._backgroundRunnable);
            object? resultCallback = self.Consume(ref self._resultCallback);

            Assert.NotNull(backgroundRunnable);
            Assert.Is<IDAsyncResultCallback<DTask<TResult>>>(resultCallback);
            self.Background(backgroundRunnable, Unsafe.As<IDAsyncResultCallback<DTask<TResult>>>(resultCallback));
        }

        public static void YieldIndirection(DAsyncFlow self)
        {
            self.RunIndirection(Yield);
        }

        public static void DelayIndirection(DAsyncFlow self)
        {
            self.RunIndirection(Delay);
        }

        public static void CallbackIndirection(DAsyncFlow self)
        {
            self.RunIndirection(Callback);
        }
    }

    private delegate void FlowContinuation(DAsyncFlow self);
}
