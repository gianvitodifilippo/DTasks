using System.Diagnostics;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;
using DTasks.Inspection;
using Xunit.Sdk;

namespace DTasks.Infrastructure.Fakes;

internal abstract class DehydratedRunnable
{
    public abstract DAsyncId ParentId { get; }

    public abstract DAsyncLink Resume(IHydrationContext context);

    public abstract DAsyncLink Resume<TResult>(IHydrationContext context, TResult result);

    public abstract DAsyncLink Resume(IHydrationContext context, Exception exception);

    public abstract bool Link(ILinkContext context);
}

internal sealed class DehydratedRunnable<TStateMachine>(
    IStateMachineInspector inspector,
    IDAsyncSurrogator surrogator,
    DAsyncId parentId) : DehydratedRunnable
    where TStateMachine : notnull
{
    private DAsyncId _parentId = parentId;
    private readonly Dictionary<string, object?> _values = [];

    public override DAsyncId ParentId => _parentId;

    public void Suspend(IDehydrationContext context, ref TStateMachine stateMachine)
    {
        FakeStateMachineWriter writer = new(_values, surrogator);

        var suspender = (IFakeStateMachineSuspender<TStateMachine>)inspector.GetSuspender(typeof(TStateMachine));
        suspender.Suspend(ref stateMachine, context, writer);
    }

    public override DAsyncLink Resume(IHydrationContext context)
    {
        FakeStateMachineReader reader = new(_values, surrogator);

        var resumer = (IFakeStateMachineResumer)inspector.GetResumer(typeof(TStateMachine));
        IDAsyncRunnable runnable = resumer.Resume(reader);

        return new DAsyncLink(_parentId, runnable);
    }

    public override DAsyncLink Resume<TResult>(IHydrationContext context, TResult result)
    {
        FakeStateMachineReader reader = new(_values, surrogator);

        var resumer = (IFakeStateMachineResumer)inspector.GetResumer(typeof(TStateMachine));
        IDAsyncRunnable runnable = resumer.Resume(reader, result);

        return new DAsyncLink(_parentId, runnable);
    }

    public override DAsyncLink Resume(IHydrationContext context, Exception exception)
    {
        FakeStateMachineReader reader = new(_values, surrogator);

        var resumer = (IFakeStateMachineResumer)inspector.GetResumer(typeof(TStateMachine));
        IDAsyncRunnable runnable = resumer.Resume(reader, exception);

        return new DAsyncLink(_parentId, runnable);
    }

    public override bool Link(ILinkContext context)
    {
        if (_parentId != default)
            throw FailException.ForFailure("Runnable was already linked.");
        
        _parentId = context.ParentId;
        return true;
    }
}

internal abstract class DehydratedCompleteRunnable : DehydratedRunnable
{
    public sealed override DAsyncId ParentId => default;

    public sealed override DAsyncLink Resume(IHydrationContext context)
    {
        throw FailException.ForFailure("Cannot resume the runnable since it has already completed.");
    }

    public sealed override DAsyncLink Resume<TResult>(IHydrationContext context, TResult result)
    {
        throw FailException.ForFailure("Cannot resume the runnable since it has already completed.");
    }

    public sealed override DAsyncLink Resume(IHydrationContext context, Exception exception)
    {
        throw FailException.ForFailure("Cannot resume the runnable since it has already completed.");
    }
}

internal sealed class DehydratedResult : DehydratedCompleteRunnable
{
    public override bool Link(ILinkContext context)
    {
        context.SetResult();
        return false;
    }
}

internal sealed class DehydratedResult<TResult>(TResult result) : DehydratedCompleteRunnable
{
    public override bool Link(ILinkContext context)
    {
        context.SetResult(result);
        return false;
    }
}

internal sealed class DehydratedException(Exception exception) : DehydratedCompleteRunnable
{
    public override bool Link(ILinkContext context)
    {
        context.SetException(exception);
        return false;
    }
}
