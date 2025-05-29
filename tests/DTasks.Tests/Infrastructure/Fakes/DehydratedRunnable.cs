using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;
using DTasks.Inspection;

namespace DTasks.Infrastructure.Fakes;

internal abstract class DehydratedRunnable
{
    public abstract DAsyncId ParentId { get; }

    public abstract DAsyncLink Resume(IResumptionContext context);

    public abstract DAsyncLink Resume<TResult>(IResumptionContext context, TResult result);

    public abstract DAsyncLink Resume(IResumptionContext context, Exception exception);
}

internal sealed class DehydratedRunnable<TStateMachine>(
    IStateMachineInspector inspector,
    IDAsyncSurrogator surrogator,
    DAsyncId parentId) : DehydratedRunnable
    where TStateMachine : notnull
{
    private readonly Dictionary<string, object?> _values = [];

    public override DAsyncId ParentId => parentId;

    public void Suspend(ISuspensionContext context, ref TStateMachine stateMachine)
    {
        FakeStateMachineWriter writer = new(_values, surrogator);

        var converter = (IFakeStateMachineSuspender<TStateMachine>)inspector.GetSuspender(typeof(TStateMachine));
        converter.Suspend(ref stateMachine, context, writer);
    }

    public override DAsyncLink Resume(IResumptionContext context)
    {
        FakeStateMachineReader reader = new(_values, surrogator);

        var converter = (IFakeStateMachineResumer)inspector.GetResumer(typeof(TStateMachine));
        IDAsyncRunnable runnable = converter.Resume(reader);

        return new DAsyncLink(parentId, runnable);
    }

    public override DAsyncLink Resume<TResult>(IResumptionContext context, TResult result)
    {
        FakeStateMachineReader reader = new(_values, surrogator);

        var converter = (IFakeStateMachineResumer)inspector.GetResumer(typeof(TStateMachine));
        IDAsyncRunnable runnable = converter.Resume(reader, result);

        return new DAsyncLink(parentId, runnable);
    }

    public override DAsyncLink Resume(IResumptionContext context, Exception exception)
    {
        throw new NotImplementedException();
    }
}
