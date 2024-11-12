using DTasks.Utils;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DTasks.Hosting;

internal sealed partial class DAsyncFlow
{
    private sealed class SuspendedDTask : DTask
    {
        public static readonly SuspendedDTask Instance = new();

        private SuspendedDTask() { }

        public override DTaskStatus Status => DTaskStatus.Suspended;

        protected override void Run(IDAsyncFlow flow) => RunCore(flow, typeof(Awaiter));

        internal static void RunCore(IDAsyncFlow flow, Type awaiterType)
        {
            Assert.Is<DAsyncFlow>(flow);
            DAsyncFlow flowImpl = Unsafe.As<DAsyncFlow>(flow);

            IDAsyncStateMachine? stateMachine = flowImpl.Consume(ref flowImpl._stateMachine);
            Assert.NotNull(stateMachine);

            flowImpl._continuation = Continuations.Return;
            flowImpl._suspendingAwaiterOrType = awaiterType;

            stateMachine.Suspend();
        }
    }

    private sealed class SuspendedDTask<TResult> : DTask<TResult>
    {
        public static readonly SuspendedDTask<TResult> Instance = new();

        private SuspendedDTask() { }

        public override DTaskStatus Status => DTaskStatus.Suspended;

        protected override void Run(IDAsyncFlow flow) => SuspendedDTask.RunCore(flow, typeof(Awaiter));
    }
}
