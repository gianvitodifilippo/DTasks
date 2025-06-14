using DTasks.Utils;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow
{
    private interface IHandleBuilder
    {
        void SetResult(DAsyncFlow flow);
        
        void SetResult<TResult>(DAsyncFlow flow, TResult result);
        
        void SetException(DAsyncFlow flow, Exception exception);
    }

    private sealed class HandleBuilder : IHandleBuilder
    {
        public static readonly HandleBuilder Instance = new();
        
        private HandleBuilder()
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
            object resultBuilder = ConsumeNotNull(ref flow._handleResultBuilder);
            return Reinterpret.Cast<IDAsyncResultBuilder>(resultBuilder);
        }
    }

    private sealed class HandleBuilder<THandleResult> : IHandleBuilder
    {
        public static readonly HandleBuilder<THandleResult> Instance = new();

        private HandleBuilder()
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
            object resultBuilder = ConsumeNotNull(ref flow._handleResultBuilder);
            return Reinterpret.Cast<IDAsyncResultBuilder<THandleResult>>(resultBuilder);
        }
    }
}