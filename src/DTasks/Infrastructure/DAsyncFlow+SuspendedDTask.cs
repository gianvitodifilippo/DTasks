using DTasks.Utils;
using System.Runtime.CompilerServices;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow
{
    private sealed class SuspendedDTask : DTask
    {
        public static readonly SuspendedDTask Instance = new();

        private SuspendedDTask() { }

        public override DTaskStatus Status => DTaskStatus.Suspended;

        protected override void Run(IDAsyncRunner runner) => RunCore(runner, typeof(Awaiter));

        internal static void RunCore(IDAsyncRunner runner, Type awaiterType)
        {
            Assert.Is<DAsyncFlow>(runner);
            DAsyncFlow flow = Unsafe.As<DAsyncFlow>(runner);

            IDAsyncStateMachine? stateMachine = Consume(ref flow._stateMachine);
            Assert.NotNull(stateMachine);

            flow._continuation = Continuations.Suspend;
            flow._suspendingAwaiterOrType = awaiterType;

            stateMachine.Suspend();
        }
    }

    private sealed class SuspendedDTask<TResult> : DTask<TResult>
    {
        public static readonly SuspendedDTask<TResult> Instance = new();

        private SuspendedDTask() { }

        public override DTaskStatus Status => DTaskStatus.Suspended;

        protected override void Run(IDAsyncRunner runner) => SuspendedDTask.RunCore(runner, typeof(Awaiter));
    }
}
