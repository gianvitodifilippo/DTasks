using DTasks.Utils;
using System.Runtime.CompilerServices;

namespace DTasks.Infrastructure;

internal partial class DAsyncFlow
{
    private static class Continuations
    {
        public static void Return(DAsyncFlow self)
        {
            self.Return();
        }

        public static void Start(DAsyncFlow self)
        {
            self.Start();
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
            object? resultBuilder = self.Consume(ref self._resultBuilder);

            Assert.NotNull(aggregateBranches);
            Assert.Is<IDAsyncResultBuilder>(resultBuilder);
            self.WhenAll(aggregateBranches, Unsafe.As<IDAsyncResultBuilder>(resultBuilder));
        }

        public static void WhenAll<TResult>(DAsyncFlow self)
        {
            IEnumerable<IDAsyncRunnable>? aggregateBranches = self.Consume(ref self._aggregateBranches);
            object? resultBuilder = self.Consume(ref self._resultBuilder);

            Assert.NotNull(aggregateBranches);
            Assert.Is<IDAsyncResultBuilder<TResult[]>>(resultBuilder);
            self.WhenAll(aggregateBranches, Unsafe.As<IDAsyncResultBuilder<TResult[]>>(resultBuilder));
        }

        public static void WhenAny(DAsyncFlow self)
        {
            IEnumerable<IDAsyncRunnable>? aggregateBranches = self.Consume(ref self._aggregateBranches);
            object? resultBuilder = self.Consume(ref self._resultBuilder);

            Assert.NotNull(aggregateBranches);
            Assert.Is<IDAsyncResultBuilder<DTask>>(resultBuilder);
            self.WhenAny(aggregateBranches, Unsafe.As<IDAsyncResultBuilder<DTask>>(resultBuilder));
        }

        public static void WhenAny<TResult>(DAsyncFlow self)
        {
            IEnumerable<IDAsyncRunnable>? aggregateBranches = self.Consume(ref self._aggregateBranches);
            object? resultBuilder = self.Consume(ref self._resultBuilder);

            Assert.NotNull(aggregateBranches);
            Assert.Is<IDAsyncResultBuilder<DTask<TResult>>>(resultBuilder);
            self.WhenAny(aggregateBranches, Unsafe.As<IDAsyncResultBuilder<DTask<TResult>>>(resultBuilder));
        }

        public static void Background(DAsyncFlow self)
        {
            IDAsyncRunnable? backgroundRunnable = self.Consume(ref self._aggregateRunnable);
            object? resultBuilder = self.Consume(ref self._resultBuilder);

            Assert.NotNull(backgroundRunnable);
            Assert.Is<IDAsyncResultBuilder<DTask>>(resultBuilder);
            self.Background(backgroundRunnable, Unsafe.As<IDAsyncResultBuilder<DTask>>(resultBuilder));
        }

        public static void Background<TResult>(DAsyncFlow self)
        {
            IDAsyncRunnable? backgroundRunnable = self.Consume(ref self._aggregateRunnable);
            object? resultBuilder = self.Consume(ref self._resultBuilder);

            Assert.NotNull(backgroundRunnable);
            Assert.Is<IDAsyncResultBuilder<DTask<TResult>>>(resultBuilder);
            self.Background(backgroundRunnable, Unsafe.As<IDAsyncResultBuilder<DTask<TResult>>>(resultBuilder));
        }

        //public static void Handle(DAsyncFlow self)
        //{
        //    DAsyncId id = self.Consume(ref self._handleId);
        //    object? resultBuilder = self.Consume(ref self._resultBuilder);

        //    Assert.Is<IDAsyncResultBuilder>(resultBuilder);
        //    self.Handle(id, Unsafe.As<IDAsyncResultBuilder>(resultBuilder));
        //}

        //public static void Handle<TResult>(DAsyncFlow self)
        //{
        //    DAsyncId id = self.Consume(ref self._handleId);
        //    object? resultBuilder = self.Consume(ref self._resultBuilder);

        //    Assert.Is<IDAsyncResultBuilder<TResult>>(resultBuilder);
        //    self.Handle(id, Unsafe.As<IDAsyncResultBuilder<TResult>>(resultBuilder));
        //}

        public static void HandleWrapper(DAsyncFlow self)
        {
            IDAsyncRunnable? runnable = self.Consume(ref self._aggregateRunnable);

            Assert.NotNull(runnable);
            runnable.Run(self);
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

        public static void SuspendBranch(DAsyncFlow self)
        {
            self.SuspendBranch();
        }
    }

    private delegate void FlowContinuation(DAsyncFlow self);
}
