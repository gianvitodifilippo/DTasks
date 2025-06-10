using System.Runtime.CompilerServices;
using DTasks.Inspection;
using DTasks.Utils;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow
{
    private interface IHandleResultHandler
    {
        void SetResult(DAsyncFlow flow);
        
        void SetResult<TResult>(DAsyncFlow flow, TResult result);
        
        void SetException(DAsyncFlow flow, Exception exception);
    }

    private sealed class HandleResultHandler : IHandleResultHandler
    {
        public static readonly HandleResultHandler Instance = new();
        
        private HandleResultHandler()
        {
        }
        
        public void SetResult(DAsyncFlow flow)
        {
            IDAsyncResultBuilder resultBuilder = ConsumeResultBuilder(flow);
            resultBuilder.SetResult();
        }

        public void SetResult<TResult>(DAsyncFlow flow, TResult result)
        {
            IDAsyncResultBuilder resultBuilder = ConsumeResultBuilder(flow);
            resultBuilder.SetResult();
        }

        public void SetException(DAsyncFlow flow, Exception exception)
        {
            IDAsyncResultBuilder resultBuilder = ConsumeResultBuilder(flow);
            resultBuilder.SetException(exception);
        }

        private static IDAsyncResultBuilder ConsumeResultBuilder(DAsyncFlow flow)
        {
            object resultBuilder = ConsumeNotNull(ref flow._resultBuilder);
            return Reinterpret.Cast<IDAsyncResultBuilder>(resultBuilder);
        }
    }

    private sealed class HandleResultHandler<THandleResult> : IHandleResultHandler
    {
        public static readonly HandleResultHandler<THandleResult> Instance = new();

        private HandleResultHandler()
        {
        }
        
        public void SetResult(DAsyncFlow flow)
        {
            _ = ConsumeResultBuilder(flow);
            
            throw new InvalidOperationException($"Handle should have been resumed with result of type '{typeof(THandleResult).FullName}'.");
        }

        public void SetResult<TResult>(DAsyncFlow flow, TResult result)
        {
            IDAsyncResultBuilder<THandleResult> resultBuilder = ConsumeResultBuilder(flow);
            
            if (result is not THandleResult handleResult)
                throw new InvalidOperationException($"Handle should have been resumed with result of type '{typeof(THandleResult).FullName}'.");
            
            resultBuilder.SetResult(handleResult);
        }

        public void SetException(DAsyncFlow flow, Exception exception)
        {
            IDAsyncResultBuilder<THandleResult> resultBuilder = ConsumeResultBuilder(flow);
            resultBuilder.SetException(exception);
        }

        private static IDAsyncResultBuilder<THandleResult> ConsumeResultBuilder(DAsyncFlow flow)
        {
            object resultBuilder = ConsumeNotNull(ref flow._resultBuilder);
            return Reinterpret.Cast<IDAsyncResultBuilder<THandleResult>>(resultBuilder);
        }
    }
    
    
    private struct CompletedRunnableBuilder
    {
        public IDAsyncRunnable Task { get; private set; }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
        {
            // TODO: Inspector should support non-generic start method
            Assert.Is<CompletedStateMachine>(stateMachine);

            Task = DTask.CompletedDTask;
        }

        public static CompletedRunnableBuilder Create() => default;
    }
    
    private struct CompletedRunnableBuilder<TResult>
    {
        public IDAsyncRunnable Task { get; private set; }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
        {
            // TODO: Inspector should support non-generic start method
            Assert.Is<CompletedStateMachine<TResult>>(stateMachine);

            TResult result = Unsafe.As<TStateMachine, CompletedStateMachine<TResult>>(ref stateMachine).Result;
            Task = DTask.FromResult(result);
        }

        public static CompletedRunnableBuilder<TResult> Create() => default;
    }
    
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    private struct CompletedStateMachine
    {
        [DAsyncRunnableBuilderField]
        public CompletedRunnableBuilder Builder;
    }
    
    private struct CompletedStateMachine<TResult>
    {
        [DAsyncRunnableBuilderField]
        public CompletedRunnableBuilder<TResult> Builder;
        
        public TResult Result;
    }
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
}